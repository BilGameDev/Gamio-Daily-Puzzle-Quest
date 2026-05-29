using Gamio.Core;
using UnityEngine;

public class SolvedHandler : MonoBehaviour
{
    IGame game;

    public void Setup(IGame currentGame)
    {
        game = currentGame;
        game.OnSolved += Solved;
    }

    void Solved()
    {
        Debug.Log("Solved");
    }

    void OnDestroy()
    {
        if (game != null)
        {
            game.OnSolved -= Solved;
        }
    }
}

