const DANGEROUS_IDENTIFIER_CHARS = /[<>"'`;\\\u0000-\u001F\u007F]/g;

export function sanitizeIdentifier(value: string): string {
  return value.trim().replace(DANGEROUS_IDENTIFIER_CHARS, '');
}

export function sanitizeEmail(value: string): string {
  return sanitizeIdentifier(value).toLowerCase();
}
