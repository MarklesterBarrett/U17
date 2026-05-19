import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";

class SiteFoundationColorTokenModalElement extends LitElement {
  static properties = {
    modalContext: { attribute: false },
    _token: { state: true },
    _paletteGroups: { state: true },
  };

  static styles = css`
    :host {
      display: block;
      color: var(--uui-color-text, #1b1b1f);
      font-family: var(--uui-font-family, Arial, sans-serif);
    }

    .layout {
      display: grid;
      grid-template-rows: auto minmax(0, 1fr) auto;
      height: 100%;
    }

    header {
      padding: 16px 20px 12px;
      border-bottom: 1px solid #e5e5e8;
    }

    h1 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
    }

    .eyebrow {
      margin: 0 0 4px;
      color: #6b6f76;
      font-size: 0.75rem;
    }

    .body {
      overflow: auto;
      padding: 16px 20px;
      display: grid;
      gap: 16px;
      align-content: start;
    }

    .field {
      display: grid;
      gap: 6px;
    }

    label {
      font-size: 0.9rem;
      font-weight: 600;
    }

    input,
    select {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid #c9c9cf;
      border-radius: 4px;
      font: inherit;
      color: inherit;
      background: #fff;
    }

    input[type="color"] {
      width: 42px;
      height: 38px;
      padding: 0;
      border: 0;
      background: transparent;
    }

    .help {
      color: #6b6f76;
      font-size: 12px;
    }

    .inline {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .preview {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 12px;
      border: 1px solid #e5e5e8;
      border-radius: 4px;
      background: #fff;
    }

    .swatch {
      width: 24px;
      height: 24px;
      border-radius: 4px;
      border: 1px solid rgba(26, 34, 48, 0.16);
      box-sizing: border-box;
    }

    .preview-meta {
      color: #6b6f76;
      font-size: 12px;
    }

    details {
      border-top: 1px solid #e5e5e8;
      padding-top: 12px;
    }

    .meta-grid {
      display: grid;
      gap: 8px;
      margin-top: 12px;
    }

    .meta-item {
      padding: 10px 12px;
      border: 1px solid #e5e5e8;
      border-radius: 4px;
      background: #fff;
    }

    .meta-item strong {
      display: block;
      margin-bottom: 4px;
      color: #6b6f76;
      font-size: 12px;
      font-weight: 600;
    }

    .palette-grid {
      display: grid;
      gap: 10px;
    }

    .palette-row {
      display: grid;
      grid-template-columns: 84px minmax(0, 1fr);
      gap: 10px;
      align-items: start;
    }

    .palette-family {
      color: #6b6f76;
      font-size: 12px;
      padding-top: 6px;
    }

    .palette-swatches {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(34px, 1fr));
      gap: 6px;
    }

    .palette-button {
      display: grid;
      gap: 4px;
      justify-items: center;
      padding: 0;
      border: 0;
      background: none;
      cursor: pointer;
    }

    .palette-chip {
      width: 32px;
      height: 32px;
      border-radius: 4px;
      border: 1px solid rgba(26, 34, 48, 0.14);
      box-sizing: border-box;
    }

    .palette-button.selected .palette-chip {
      outline: 2px solid #3544b1;
      outline-offset: 1px;
    }

    .palette-shade {
      color: #6b6f76;
      font-size: 10px;
      line-height: 1;
    }

    footer {
      position: sticky;
      bottom: 0;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
      padding: 12px 20px;
      border-top: 1px solid #e5e5e8;
      background: #fff;
    }

    .actions {
      display: flex;
      gap: 8px;
      margin-left: auto;
    }

    @media (max-width: 720px) {
      header,
      .body,
      footer {
        padding-left: 16px;
        padding-right: 16px;
      }
      .palette-row {
        grid-template-columns: 1fr;
      }
    }
  `;

  constructor() {
    super();
    this.modalContext = undefined;
    this._token = null;
    this._paletteGroups = [];
    this._initializedFromContext = false;
  }

  willUpdate(changedProperties) {
    if (changedProperties.has("modalContext") && this.modalContext && !this._initializedFromContext) {
      this._initializedFromContext = true;
      this._token = this._clone(this.modalContext.data?.token || null);
      this._syncToken();
      this._paletteGroups = this._groupPaletteOptions(this.modalContext.data?.paletteOptions || []);
    }
  }

  _clone(value) {
    return value ? JSON.parse(JSON.stringify(value)) : null;
  }

  _normalizeName(value) {
    return String(value || "")
      .trim()
      .toLowerCase()
      .replace(/[\s._]+/g, "-")
      .replace(/[^a-z0-9-]/g, "")
      .replace(/-{2,}/g, "-")
      .replace(/^-+|-+$/g, "");
  }

  _groupPaletteOptions(options) {
    const groups = new Map();
    options.forEach((option) => {
      const alias = String(option.alias || "");
      const match = alias.match(/^([a-z]+)-(\d{2,3})$/i);
      const family = match ? this._friendlyLabel(match[1]) : option.label?.replace(/\s+\d+$/, "") || option.label || alias;
      const shade = match ? match[2] : alias.split("-").slice(1).join("-") || "";
      if (!groups.has(family)) {
        groups.set(family, []);
      }
      groups.get(family).push({ ...option, shade });
    });

    return Array.from(groups.entries())
      .map(([family, items]) => ({
        family,
        items: items.sort((left, right) => String(left.shade).localeCompare(String(right.shade), undefined, { numeric: true })),
      }))
      .sort((left, right) => left.family.localeCompare(right.family));
  }

  _friendlyLabel(value) {
    const text = String(value || "").replace(/[-_.]+/g, " ").trim();
    return text.replace(/\b\w/g, (char) => char.toUpperCase());
  }

  _syncToken() {
    if (!this._token) {
      return;
    }

    if (this._token.editorKind === "paletteColor") {
      const option = (this.modalContext?.data?.paletteOptions || []).find((item) => item.alias === this._token.paletteAlias) || null;
      this._token.label = option?.label || this._token.label || this._token.paletteAlias || "";
      this._token.path = `color.${this._token.paletteAlias || ""}`;
      this._token.cssVariable = `--color-${this._token.paletteAlias || ""}`;
      this._token.value = option?.value || this._token.value || "";
      this._token.rawValue = String(this._token.value || "");
      this._token.resolvedValue = this._token.rawValue;
      return;
    }

    const normalized = this._normalizeName(this._token.name || this._token.label || "custom-colour");
    this._token.name = normalized;
    this._token.label = this._token.label || normalized;
    this._token.path = `color.${normalized}`;
    this._token.cssVariable = `--color-${normalized}`;
    this._token.rawValue = String(this._token.value || "");
    this._token.resolvedValue = this._token.rawValue;
  }

  _setField(field, value) {
    if (!this._token) {
      return;
    }

    this._token = {
      ...this._token,
      [field]: value,
    };
    this._syncToken();
    this.requestUpdate();
  }

  _handleCancel() {
    this.modalContext?.reject();
  }

  _handleDelete() {
    if (!this._token) {
      return;
    }

    this.modalContext?.updateValue({
      action: "delete",
      tokenId: this._token.id,
    });
    this.modalContext?.submit();
  }

  _handleSave() {
    if (!this._token) {
      return;
    }

    this.modalContext?.updateValue({
      action: "save",
      token: this._clone(this._token),
    });
    this.modalContext?.submit();
  }

  _renderPreview() {
    if (!this._token) {
      return "";
    }

    return html`
      <div class="preview">
        <span class="swatch" style="background:${this._token.value || "#ffffff"};"></span>
        <div>
          <div><strong>${this._token.label || "Colour token"}</strong></div>
          <div class="preview-meta">${this._token.value || "Not set"}</div>
        </div>
      </div>
    `;
  }

  _renderPalettePicker() {
    if (!this._paletteGroups.length) {
      return "";
    }

    return html`
      <div class="palette-grid">
        ${this._paletteGroups.map((group) => html`
          <div class="palette-row">
            <div class="palette-family">${group.family}</div>
            <div class="palette-swatches">
              ${group.items.map((option) => html`
                <button
                  type="button"
                  class="palette-button ${this._token?.paletteAlias === option.alias ? "selected" : ""}"
                  title=${option.label || option.alias}
                  @click=${() => this._setField("paletteAlias", option.alias)}>
                  <span class="palette-chip" style="background:${option.value || "#ffffff"};"></span>
                  <span class="palette-shade">${option.shade || ""}</span>
                </button>`)}
            </div>
          </div>`)}
      </div>
    `;
  }

  render() {
    const token = this._token;
    const mode = this.modalContext?.data?.mode || "edit";
    const title = mode === "palette-color"
      ? "Add Palette Colour"
      : mode === "custom-color"
        ? "Add Custom Colour"
        : `Edit Colour - ${token?.label || ""}`;

    if (!token) {
      return html``;
    }

    return html`
      <div class="layout">
        <header>
          <div class="eyebrow">Foundation Tokens</div>
          <h1>${title}</h1>
        </header>

        <div class="body">
          ${token.editorKind === "paletteColor" ? html`
            <div class="field">
              <label for="palette-select">Palette value</label>
              <select id="palette-select" .value=${token.paletteAlias || ""} @change=${(event) => this._setField("paletteAlias", event.target.value)}>
                ${(this.modalContext?.data?.paletteOptions || []).map((option) => html`
                  <option value=${option.alias}>${option.label || option.alias}</option>`)}
              </select>
              <div class="help">Choose a built-in palette colour.</div>
            </div>
            ${this._renderPalettePicker()}
          ` : html`
            <div class="field">
              <label for="token-label">Label</label>
              <input id="token-label" .value=${token.label || ""} @change=${(event) => this._setField("label", event.target.value)} />
              <div class="help">Editor-facing name shown in the list.</div>
            </div>
            <div class="field">
              <label for="token-name">Token name</label>
              <input id="token-name" .value=${token.name || ""} @change=${(event) => this._setField("name", event.target.value)} />
              <div class="help">Used in the generated token path.</div>
            </div>
            <div class="field">
              <label for="token-value">Colour value</label>
              <div class="inline">
                ${String(token.value || "").match(/^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i)
                  ? html`<input type="color" .value=${token.value || "#000000"} @change=${(event) => this._setField("value", event.target.value)} />`
                  : ""}
                <input id="token-value" .value=${token.value || ""} @change=${(event) => this._setField("value", event.target.value)} />
              </div>
            </div>
          `}

          ${this._renderPreview()}

          <details>
            <summary>View details</summary>
            <div class="meta-grid">
              <div class="meta-item"><strong>Property alias</strong><div>${token.propertyAlias || ""}</div></div>
              <div class="meta-item"><strong>Token path</strong><div>${token.path || ""}</div></div>
              <div class="meta-item"><strong>CSS variable</strong><div>${token.cssVariable || ""}</div></div>
              <div class="meta-item"><strong>Source</strong><div>${token.editorKind || ""}</div></div>
              <div class="meta-item"><strong>Raw value</strong><div>${token.rawValue || ""}</div></div>
              <div class="meta-item"><strong>Resolved value</strong><div>${token.resolvedValue || ""}</div></div>
            </div>
          </details>
        </div>

        <footer>
          <uui-button look="secondary" @click=${this._handleCancel}>Cancel</uui-button>
          <div class="actions">
            ${token.removable ? html`<uui-button look="outline" color="danger" @click=${this._handleDelete}>Delete</uui-button>` : ""}
            <uui-button look="primary" @click=${this._handleSave}>Save</uui-button>
          </div>
        </footer>
      </div>
    `;
  }
}

customElements.define("site-foundation-color-token-modal", SiteFoundationColorTokenModalElement);

export const element = SiteFoundationColorTokenModalElement;
export default SiteFoundationColorTokenModalElement;
