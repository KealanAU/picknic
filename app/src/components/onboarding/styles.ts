import { t } from '../../theme/tokens';

// The one tray heading — every sheet title in the app uses this scale.
// (Screen headers are PartyHeader's 34px; trays sit one step below.)
export const titleStyle = {
  fontFamily: t.font.display,
  fontSize: '26px',
  fontWeight: '300',
  lineHeight: '1.05',
  letterSpacing: t.tracking,
  color: t.color.ink,
} as const;

export const subStyle = {
  fontFamily: t.font.body,
  fontSize: '14px',
  letterSpacing: t.trackingSmall,
  color: t.color.muted,
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
