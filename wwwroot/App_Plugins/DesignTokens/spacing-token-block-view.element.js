const DEVICE_ICONS = {
  mobile: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" aria-hidden="true"><rect width="14" height="20" x="5" y="2" rx="2" ry="2"></rect><path d="M12 18h.01"></path></svg>`,
  tablet: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" aria-hidden="true"><rect width="16" height="20" x="4" y="2" rx="2" ry="2"></rect><path d="M12 18h.01"></path></svg>`,
  desktop: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" aria-hidden="true"><rect width="20" height="14" x="2" y="3" rx="2"></rect><path d="M8 21h8M12 17v4"></path></svg>`
};

class MySpacingTokenBlockView extends HTMLElement {
  constructor() {
    super();
    this._content = null;
    this._config = null;
    this.attachShadow({ mode: "open" });
  }

  set content(value) {
    this._content = value;
    this.render();
  }

  get content() {
    return this._content;
  }

  set config(value) {
    this._config = value;
    this.render();
  }

  get config() {
    return this._config;
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const name = this.readValue(this.content, "name") || this.readValue(this.content, "label");
    const alias = this.createSpacingAlias(name || this.readValue(this.content, "alias"));
    const responsiveValues = this.readResponsiveValues(this.content);
    const mobile = responsiveValues.mobile || "-";
    const tablet = responsiveValues.tablet || "-";
    const desktop = responsiveValues.desktop || "-";
    const editPath = this.config?.editContentPath || "";
    const content = `
      ${alias ? `<span class="alias">${this.escapeHtml(alias)}</span>` : ""}
      <span class="values">
        ${this.renderValue("Mobile", "mobile", mobile)}
        ${this.renderValue("Tablet", "tablet", tablet)}
        ${this.renderValue("Desktop", "desktop", desktop)}
      </span>
    `;

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          box-sizing: border-box;
        }

        .spacing-token {
          display: flex;
          align-items: center;
          justify-content: flex-start;
          gap: 14px;
          min-height: 48px;
          box-sizing: border-box;
          color: inherit;
          text-decoration: none;
        }

        .alias {
          flex: 0 0 25%;
          max-width: 25%;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          color: rgba(0, 0, 0, 0.58);
          font-size: 12px;
          line-height: 1.2;
        }

        .values {
          display: flex;
          align-items: center;
          justify-content: flex-start;
          gap: 12px;
          flex-wrap: wrap;
        }

        .value {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          min-height: 34px;
          padding: 0;
          box-sizing: border-box;
          color: rgba(0, 0, 0, 0.78);
          font-size: 12px;
          font-weight: 600;
          line-height: 1;
          white-space: nowrap;
        }

        .value svg {
          width: 18px;
          height: 18px;
          flex: 0 0 18px;
        }

        .value-text {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          width: 48px;
          min-height: 34px;
          box-sizing: border-box;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: rgba(0, 0, 0, 0.78);
        }
      </style>
      ${editPath
        ? `<a class="spacing-token" href="${this.escapeAttr(editPath)}">${content}</a>`
        : `<div class="spacing-token">${content}</div>`}
    `;
  }

  renderValue(label, icon, value) {
    return `
      <span class="value" title="${this.escapeAttr(label)}">
        ${DEVICE_ICONS[icon]}
        <span class="value-text">${this.escapeHtml(value)}</span>
      </span>
    `;
  }

  readValue(source, alias) {
    const value = source?.[alias];

    if (Array.isArray(value)) {
      return this.readValue({ value: value[0] }, "value");
    }

    if (typeof value === "string") {
      return value;
    }

    if (value && typeof value === "object") {
      return value.value || value.alias || value.name || "";
    }

    return "";
  }

  readResponsiveValues(source) {
    const value = this.readValue(source, "responsiveValues");

    if (!value) {
      return {};
    }

    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === "object" ? parsed : {};
    } catch {
      return {};
    }
  }

  createSpacingAlias(name) {
    const normalizedName = String(name || "").trim();

    if (!normalizedName) {
      return "";
    }

    if (/^(0|none|space-none)$/i.test(normalizedName)) {
      return "space-none";
    }

    if (/^space-/i.test(normalizedName)) {
      return normalizedName.toLowerCase();
    }

    const slug = normalizedName
      .toLowerCase()
      .split(/[\s_-]+/)
      .filter(Boolean)
      .join("-");

    return slug ? `space-${slug}` : "";
  }

  escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  escapeAttr(value) {
    return this.escapeHtml(value);
  }
}

if (!customElements.get("my-spacing-token-block-view")) {
  customElements.define("my-spacing-token-block-view", MySpacingTokenBlockView);
}
