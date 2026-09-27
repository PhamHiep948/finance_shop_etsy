export function BrandIcon({ size = 40, className = "" }) {
  return (
    <div
      className={`sb-brand-icon-img ${className}`.trim()}
      style={{ width: size, height: size }}
      aria-hidden="true"
    >
      <img
        src="/assets/mina-store-logo.jpg"
        alt="Mina Store Logo"
        width={size}
        height={size}
        className="brand-logo-img"
        loading="eager"
      />
    </div>
  );
}

export function BrandLogo({
  iconSize = 40,
  showSub = true,
  subText = "Quản lý tài chính",
  className = "",
}) {
  return (
    <div className={`sb-brand-wrap ${className}`.trim()}>
      <BrandIcon size={iconSize} />
      <div className="sb-brand-text">
        <span className="sb-brand-name">
          <span className="brand-name-mina">Mina</span>
          <span className="brand-name-store">Store</span>
        </span>
        {showSub && (
          <span className="sb-brand-sub">
            <span className="brand-sub-dot" aria-hidden="true" />
            <span>{subText}</span>
          </span>
        )}
      </div>
    </div>
  );
}

export default BrandLogo;
