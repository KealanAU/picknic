import { t } from '../../theme/tokens';

export const titleStyle = {
  fontFamily: t.font.display,
  fontSize: '30px',
  fontWeight: '400',
  lineHeight: '1.05',
  letterSpacing: t.tracking,
  color: t.color.ink,
} as const;

export const subStyle = {
  fontFamily: t.font.body,
  fontSize: '14px',
  letterSpacing: t.tracking,
  color: t.color.muted,
} as const;

export const errStyle = {
  fontFamily: t.font.body,
  fontSize: '14px',
  letterSpacing: t.tracking,
  color: t.color.danger,
  marginTop: '6px',
} as const;

export const backButtonStyle = {
  alignSelf: 'flex-start',
  marginTop: '-8px',
  marginBottom: '10px',
} as const;

export const headerStyle = {
  display: 'flex',
  flexDirection: 'column',
  gap: '4px',
  marginBottom: '10px',
} as const;

export const primaryActionStyle = {
  marginTop: '10px',
} as const;
