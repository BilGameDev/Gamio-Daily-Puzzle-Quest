interface GoogleTokenPayload {
  sub: string;
  email: string;
  name?: string;
  picture?: string;
  aud: string;
  exp: number;
  iat: number;
}

let cachedCerts: { [kid: string]: CryptoKey } | null = null;
let certsExpiry = 0;

async function fetchGoogleCerts(): Promise<{ [kid: string]: CryptoKey }> {
  if (cachedCerts && Date.now() / 1000 < certsExpiry) {
    return cachedCerts;
  }

  const resp = await fetch('https://www.googleapis.com/oauth2/v3/certs');
  const data: any = await resp.json();

  const keys: { [kid: string]: CryptoKey } = {};
  for (const key of data.keys) {
    // Strip to only fields needed for RSA JWK import
    // (Google returns extra fields like x5c/x5t that some runtimes reject)
    const jwk: any = {
      kty: key.kty,
      n: key.n,
      e: key.e,
      alg: 'RS256',
    };

    const cryptoKey = await crypto.subtle.importKey(
      'jwk', jwk,
      { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' },
      false, ['verify']
    );
    keys[key.kid] = cryptoKey;
  }

  cachedCerts = keys;
  certsExpiry = Math.floor(Date.now() / 1000) + 3600;
  return keys;
}

export async function verifyGoogleToken(
  idToken: string, clientId: string
): Promise<GoogleTokenPayload | null> {
  try {
    const parts = idToken.split('.');
    if (parts.length !== 3) return null;

    const header = JSON.parse(atob(parts[0]));
    const kid = header.kid;

    const keys = await fetchGoogleCerts();
    const key = keys[kid];
    if (!key) return null;

    const data = new TextEncoder().encode(`${parts[0]}.${parts[1]}`);
    const sig = base64urlToBytes(parts[2]);

    const valid = await crypto.subtle.verify('RSASSA-PKCS1-v1_5', key, sig, data);
    if (!valid) return null;

    const payload: GoogleTokenPayload = JSON.parse(atob(parts[1]));

    if (payload.aud !== clientId) return null;
    if (payload.exp < Math.floor(Date.now() / 1000)) return null;

    return payload;
  } catch (err) {
    console.error('[auth] verifyGoogleToken error:', err);
    return null;
  }
}

function base64urlToBytes(str: string): ArrayBuffer {
  str = str.replace(/-/g, '+').replace(/_/g, '/');
  while (str.length % 4) str += '=';
  const binary = atob(str);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes.buffer;
}
