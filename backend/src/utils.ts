import { Env } from './index';

export function jsonResponse(data: any, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      'Content-Type': 'application/json',
      'Access-Control-Allow-Origin': '*',
    },
  });
}

export function errorResponse(status: number, message: string): Response {
  return jsonResponse({ error: message }, status);
}

export function unauthorizedResponse(): Response {
  return errorResponse(401, 'Unauthorized');
}

// Simple HMAC-based session token (no external deps)
export async function createSessionToken(userId: string, secret: string): Promise<string> {
  const encoder = new TextEncoder();
  const issuedAt = Math.floor(Date.now() / 1000);
  const expiresAt = issuedAt + 86400 * 30; // 30 days
  const payload = `${userId}:${issuedAt}:${expiresAt}`;

  const key = await crypto.subtle.importKey(
    'raw', encoder.encode(secret),
    { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']
  );
  const sig = await crypto.subtle.sign('HMAC', key, encoder.encode(payload));
  const mac = Array.from(new Uint8Array(sig))
    .map(b => b.toString(16).padStart(2, '0')).join('');

  return `${base64urlEncode(payload)}.${mac}`;
}

export async function verifySessionToken(
  token: string, secret: string
): Promise<{ userId: string; expiresAt: number } | null> {
  try {
    const parts = token.split('.');
    if (parts.length !== 2) return null;

    const payloadBytes = base64urlDecode(parts[0]);
    const payload = new TextDecoder().decode(payloadBytes);
    const providedMac = parts[1];

    const key = await crypto.subtle.importKey(
      'raw', new TextEncoder().encode(secret),
      { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']
    );
    const sig = await crypto.subtle.sign('HMAC', key, new TextEncoder().encode(payload));
    const expectedMac = Array.from(new Uint8Array(sig))
      .map(b => b.toString(16).padStart(2, '0')).join('');

    if (providedMac !== expectedMac) return null;

    const [userId, _issuedAt, expiresAtStr] = payload.split(':');
    const expiresAt = parseInt(expiresAtStr);

    if (Date.now() / 1000 > expiresAt) return null;

    return { userId, expiresAt };
  } catch {
    return null;
  }
}

export async function getAuthUser(
  authHeader: string, jwtSecret: string, db: D1Database
): Promise<{ id: string } | null> {
  if (!authHeader.startsWith('Bearer ')) return null;

  const token = authHeader.slice(7);
  const session = await verifySessionToken(token, jwtSecret);
  if (!session) return null;

  const user = await db.prepare('SELECT id FROM users WHERE id = ?')
    .bind(session.userId).first<{ id: string }>();
  return user || null;
}

function base64urlEncode(str: string): string {
  return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function base64urlDecode(str: string): Uint8Array {
  str = str.replace(/-/g, '+').replace(/_/g, '/');
  while (str.length % 4) str += '=';
  const binary = atob(str);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}
