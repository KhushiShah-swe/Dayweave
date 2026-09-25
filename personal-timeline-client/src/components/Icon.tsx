import { CSSProperties } from 'react';
const paths: Record<string, JSX.Element> = {
  grid: (
    <>
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </>
  ),
  timeline: (
    <>
      <path d="M6 4v16M11 5h9M11 12h6M11 19h9" />
      <circle cx="6" cy="5" r="1" />
      <circle cx="6" cy="12" r="1" />
      <circle cx="6" cy="19" r="1" />
    </>
  ),
  link: (
    <>
      <path
        d="m10 13 4-4M8 16l-1 1a4 4 0 0 1-6-6l4-4a4 4 0 0 1 6 0M16 8l1-1a4 4 0 0 1 6 6l-4 4a4 4 0 0 1-6 0"
        transform="translate(0 -1)"
      />
    </>
  ),
  plus: <path d="M12 5v14M5 12h14" />,
  arrow: <path d="M4 12h16m-6-6 6 6-6 6" />,
  search: (
    <>
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="m16 16 5 5" />
    </>
  ),
  download: (
    <>
      <path d="M12 3v12m-5-5 5 5 5-5M4 16v5h16v-5" />
    </>
  ),
  GitHub: (
    <>
      <path d="m8 6-6 6 6 6m8-12 6 6-6 6m-3-14-2 16" />
    </>
  ),
  Spotify: (
    <>
      <path d="M9 17V5l11-2v12M9 7l11-2" />
      <ellipse cx="6" cy="17" rx="3" ry="2.5" />
      <ellipse cx="17" cy="15" rx="3" ry="2.5" />
    </>
  ),
  YouTube: (
    <>
      <rect x="2" y="5" width="20" height="14" rx="4" />
      <path d="m10 9 5 3-5 3z" />
    </>
  ),
  Manual: (
    <>
      <path d="m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z" />
    </>
  ),
  calendar: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M7 3v4m10-4v4M3 10h18m-12 4h1m4 0h1" />
    </>
  ),
  edit: (
    <>
      <path d="m14 5 5 5M4 20l5-1L21 7l-5-5L4 14z" />
    </>
  ),
  trash: (
    <>
      <path d="M4 6h16M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7m4-7v7" />
    </>
  ),
  close: <path d="m6 6 12 12M6 18 18 6" />,
  exit: (
    <>
      <path d="M10 4H4v16h6m5-12 4 4-4 4M9 12h10" />
    </>
  ),
  refresh: (
    <>
      <path d="M20 7v5h-5M4 17v-5h5" />
      <path d="M5 7a8 8 0 0 1 14-1l1 3M4 15l1 3a8 8 0 0 0 14-1" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  external: (
    <>
      <path d="M14 3h7v7m0-7L10 14M10 3H3v18h18v-7" />
    </>
  ),
};
export function Icon({
  name,
  size = 20,
  style,
}: {
  name: string;
  size?: number;
  style?: CSSProperties;
}) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.65"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      style={style}
    >
      {paths[name] || paths.Manual}
    </svg>
  );
}
export function Brand() {
  return (
    <span className="brand">
      <span className="brand-mark" aria-hidden="true">
        <svg viewBox="0 0 40 40">
          <path d="M10 8v24M19 8v24M28 8c14 10 0 24-9 24M10 8h9" />
        </svg>
      </span>
      dayweave<span className="brand-period">.</span>
    </span>
  );
}
