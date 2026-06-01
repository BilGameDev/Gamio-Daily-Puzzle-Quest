// <copyright file="SigninSampleScript.cs" company="Google Inc.">
// Copyright (C) 2017 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations

namespace SignInSample
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using Google;
  using TMPro;
  using UnityEngine;
  using UnityEngine.Networking;
  using UnityEngine.UI;

  public class SigninSampleScript : MonoBehaviour
  {

    [Header("UI Components")]
    public TextMeshProUGUI statusText;
    public RawImage profileImage;

    [Header("Configuration")]
    public string webClientId = "<your client id here>";

    private GoogleSignInConfiguration configuration;

    // Defer the configuration creation until Awake so the web Client ID
    // Can be set via the property inspector in the Editor.
    private void Awake()
    {

      configuration = new GoogleSignInConfiguration
      {
        WebClientId = webClientId,
        RequestIdToken = true,
        RequestEmail = true,
        RequestProfile = true
      };
    }

    public void OnSignIn()
    {
      GoogleSignIn.Configuration = configuration;
      GoogleSignIn.Configuration.UseGameSignIn = false;
      GoogleSignIn.Configuration.RequestIdToken = true;
      AddStatusText("Calling SignIn");

      GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
        OnAuthenticationFinished);
    }

    public void OnSignOut()
    {
      AddStatusText("Signing out...");
      GoogleSignIn.DefaultInstance.SignOut();
      ClearProfileImage();
      AddStatusText("Signed out successfully");
    }

    public void OnDisconnect()
    {
      AddStatusText("Disconnecting...");
      GoogleSignIn.DefaultInstance.Disconnect();
      ClearProfileImage();
      AddStatusText("Disconnected successfully");
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
      if (task.IsFaulted)
      {
        HandleSignInError(task);
      }
      else if (task.IsCanceled)
      {
        AddStatusText("Sign-in canceled by user");
      }
      else
      {
        HandleSignInSuccess(task.Result);
      }
    }

    private void HandleSignInError(Task<GoogleSignInUser> task)
    {
      using (IEnumerator<System.Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
      {
        if (enumerator.MoveNext())
        {
          GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
          AddStatusText($"Error: {error.Status}");
          AddStatusText($"Message: {error.Message}");
          Debug.LogError($"Google Sign-In Error: {error.Status} - {error.Message}");
        }
        else
        {
          AddStatusText($"Unexpected Exception: {task.Exception.Message}");
          Debug.LogError($"Unexpected Sign-In Exception: {task.Exception}");
        }
      }
    }

    private void HandleSignInSuccess(GoogleSignInUser user)
    {
      AddStatusText($"Welcome, {user.DisplayName}!");
      AddStatusText($"Email: {user.Email}");

      if (user.ImageUrl != null && !string.IsNullOrEmpty(user.ImageUrl.ToString()))
      {
        AddStatusText("Loading profile image...");
        StartCoroutine(LoadProfileImage(user.ImageUrl.ToString()));
      }
      else
      {
        AddStatusText("No profile image available");
        ClearProfileImage();
      }
    }

    private IEnumerator LoadProfileImage(string imageUrl)
    {
      using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          Texture2D texture = DownloadHandlerTexture.GetContent(request);
          if (profileImage != null)
          {
            profileImage.texture = texture;
            profileImage.enabled = true;
            AddStatusText("Profile image loaded");
            Debug.Log("Profile image loaded successfully");
          }
        }
        else
        {
          AddStatusText("Failed to load image");
          Debug.LogError($"Failed to load profile image: {request.error}");
          ClearProfileImage();
        }
      }
    }

    private void ClearProfileImage()
    {
      if (profileImage != null)
      {
        profileImage.texture = null;
        profileImage.enabled = false;
      }
    }

    public void OnSignInSilently()
    {
      GoogleSignIn.Configuration = configuration;
      GoogleSignIn.Configuration.UseGameSignIn = false;
      GoogleSignIn.Configuration.RequestIdToken = true;
      AddStatusText("Calling SignIn Silently");

      GoogleSignIn.DefaultInstance.SignInSilently()
            .ContinueWith(OnAuthenticationFinished);
    }


    public void OnGamesSignIn()
    {
      GoogleSignIn.Configuration = configuration;
      GoogleSignIn.Configuration.UseGameSignIn = true;
      GoogleSignIn.Configuration.RequestIdToken = false;

      AddStatusText("Calling Games SignIn");

      GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
        OnAuthenticationFinished);
    }

    private const int MaxStatusMessages = 5;
    private readonly List<string> messages = new List<string>();

    private void AddStatusText(string text)
    {
      if (messages.Count >= MaxStatusMessages)
      {
        messages.RemoveAt(0);
      }

      messages.Add(text);

      if (statusText != null)
      {
        statusText.text = string.Join("\n", messages);
      }

      Debug.Log($"[GoogleSignIn] {text}");
    }
  }
}
