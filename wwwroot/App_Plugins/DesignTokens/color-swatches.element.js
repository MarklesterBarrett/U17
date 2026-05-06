import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

class MyContentmentColorSwatches extends HTMLElement {
  constructor() {
    super();
    this._value = "";
    this._config = {};
    this._isOpen = false;
    this._handleDocumentPointerDown = this.handleDocumentPointerDown.bind(this);
    this.attachShadow({ mode: "open" });
  }

  set value(v) {
    this._value = v || "";
    this.render();
  }

  get value() {
    return this._value;
  }

  set config(v) {
    this._config = v || {};
    this.render();
  }

  get config() {
    return this._config || {};
  }

  get items() {
    if (typeof this.config?.getValueByAlias === "function") {
      const aliasedItems = this.config.getValueByAlias("items");
      return Array.isArray(aliasedItems) ? aliasedItems : [];
    }

    return Array.isArray(this.config.items) ? this.config.items : [];
  }

  connectedCallback() {
    this.render();
  }

  disconnectedCallback() {
    this.removeOutsideClickListener();
  }

  selectValue(nextValue) {
    this._value = nextValue;
    this._isOpen = false;
    this.removeOutsideClickListener();

    this.dispatchEvent(new UmbChangeEvent());

    this.render();
  }

  setOpen(isOpen) {
    this._isOpen = isOpen;

    if (this._isOpen) {
      this.addOutsideClickListener();
    } else {
      this.removeOutsideClickListener();
    }

    this.render();
  }

  addOutsideClickListener() {
    this.ownerDocument.addEventListener("pointerdown", this._handleDocumentPointerDown, true);
  }

  removeOutsideClickListener() {
    this.ownerDocument.removeEventListener("pointerdown", this._handleDocumentPointerDown, true);
  }

  handleDocumentPointerDown(event) {
    const path = event.composedPath();
    const clickedInsidePicker = path.some((node) => node?.classList?.contains("picker"));

    if (!clickedInsidePicker) {
      this.setOpen(false);
    }
  }

  render() {
    const items = this.items;
    const mode = this.getViewMode(items);

    const style = `
      <style>
        :host {
          display: block;
        }

        .picker {
          position: relative;
          width: min(360px, 100%);
          max-width: 100%;
        }

        .primitive-colour {
          display: flex;
          align-items: center;
          gap: 10px;
          min-height: 44px;
          box-sizing: border-box;
          color: inherit;
          text-decoration: none;
        }

        button.primitive-colour {
          width: 100%;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          cursor: pointer;
          padding: 6px 10px;
          font: inherit;
          text-align: left;
        }

        button.primitive-colour[aria-expanded="true"] {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
        }

        .swatch {
          display: inline-block;
          width: 30px;
          height: 30px;
          flex: 0 0 30px;
          box-sizing: border-box;
          border: 1px solid rgba(0, 0, 0, 0.18);
          border-radius: 4px;
        }

        .label {
          flex: 1 1 auto;
          min-width: 0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          font: inherit;
          font-weight: 600;
        }

        .chevron {
          flex: 0 0 auto;
          color: #6a6a6a;
          font-size: 12px;
        }

        .palette {
          position: absolute;
          top: calc(100% + 4px);
          left: 0;
          z-index: 1000;
          display: grid;
          gap: 8px;
          width: max(360px, 100%);
          max-width: min(640px, calc(100vw - 48px));
          max-height: min(420px, calc(100vh - 180px));
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          padding: 10px;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
          overflow-y: auto;
          overflow-x: hidden;
          box-sizing: border-box;
        }

        .grid {
          display: grid;
          grid-template-columns: 72px repeat(var(--shade-count), 22px);
          gap: 2px;
          align-items: center;
          min-width: fit-content;
        }

        .custom-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(112px, 1fr));
          gap: 6px;
          margin-top: 8px;
          max-width: 560px;
        }

        .label__color{
          font-size: 10px;
          line-height: 1;
        }

        .text-center {
          text-align: center;
 
        }
        
        .text-right {
          text-align: right;
        }

        button.swatch {
          display: block;
          width: 22px;
          height: 22px;
          cursor: pointer;
          box-sizing: border-box;
          margin: 0;
          padding: 0;
          border: 1px solid rgba(0, 0, 0, 0.18);
          border-radius: 3px;
        }


        button.swatch.selected {
          outline-color: #3879ff;
          outline-width: 2px;
          outline-style: solid;
        }

        button.custom-swatch.selected {
          border-color: #3879ff;
          outline-color: #3879ff;
          outline-width: 2px;
          outline-style: solid;
        }
      </style>
    `;

    this.shadowRoot.innerHTML = mode === "list"
      ? `${style}${this.renderList(items)}`
      : `${style}${this.renderGrid(items)}`;

    this.shadowRoot.querySelector("button.primitive-colour:not(.custom-swatch)")?.addEventListener("click", () => {
      this.setOpen(!this._isOpen);
    });

    this.shadowRoot.querySelectorAll("button.swatch, button.custom-swatch").forEach((btn) => {
      btn.addEventListener("click", () => {
        this.selectValue(btn.getAttribute("data-value") || "");
      });
    });
  }

  renderGrid(items) {
    const groupedItems = this.groupItems(items);
    const shadeGroups = groupedItems.filter((group) => group.items.some((item) => item.shade));
    const customItems = groupedItems.flatMap((group) => group.items.filter((item) => !item.shade));
    const shades = this.getShades(shadeGroups);
    const gridTop = shades.length
      ? ['<div class="label__color"></div>', ...shades.map((shade) => `<div class="label__color text-center">${this.escapeHtml(shade)}</div>`)].join("")
      : "";

    const gridHtml = shadeGroups
      .map((group) => {
        const swatches = shades
          .map((shade) => {
            const item = group.items.find((entry) => entry.shade === shade);

            if (!item) {
              return "<div></div>";
            }

            const alias = item.value || "";
            const selected = alias === this._value ? "selected" : "";
            const swatchColor = item.description || "transparent";
            const title = this.getItemTitle(item, selected);

            return `
              <button 
                class="swatch ${selected}" 
                type="button" 
                data-value="${this.escapeAttr(alias)}"
                aria-label="${this.escapeAttr(title)}" 
                title="${this.escapeAttr(title)}"
                style="background:${this.escapeAttr(swatchColor)}">
              </button>
            `;
          })
          .join("");

        return `<div class="label__color text-right">${this.escapeHtml(group.family)}</div>${swatches}`;
      })
      .join("");

    const customHtml = customItems
      .map((item) => {
        const alias = item.value || "";
        const selected = alias === this._value ? "selected" : "";
        const swatchColor = item.description || "transparent";
        const title = this.getItemTitle(item, selected);

        return `
          <button 
            class="primitive-colour custom-swatch ${selected}" 
            type="button" 
            data-value="${this.escapeAttr(alias)}"
            aria-label="${this.escapeAttr(title)}" 
            title="${this.escapeAttr(title)}">
            <span class="swatch" style="background:${this.escapeAttr(swatchColor)}"></span>
            <span class="label">${this.escapeHtml(item.name || alias)}</span>
          </button>
        `;
      })
      .join("");

    const palette = shades.length ? `<div class="grid" style="--shade-count:${shades.length}">${gridTop}${gridHtml}</div>` : "";
    const customPalette = customHtml ? `<div class="custom-grid">${customHtml}</div>` : "";

    return `${palette}${customPalette}`;
  }

  renderList(items) {
    const selectedItem = items.find((item) => (item.value || "") === this._value);
    const selectedLabel = selectedItem?.name || this._value || "Choose base colour";
    const selectedColor = selectedItem?.description || this.resolveCssColor(this._value) || "transparent";
    const selectedTitle = selectedLabel;
    const options = items
      .map((item) => {
        const alias = item.value || "";
        const selected = alias === this._value ? "selected" : "";
        const swatchColor = item.description || this.resolveCssColor(alias) || "transparent";
        const title = this.getItemTitle(item, selected);

        return `
          <button
            class="primitive-colour custom-swatch ${selected}"
            type="button"
            data-value="${this.escapeAttr(alias)}"
            aria-label="${this.escapeAttr(title)}"
            title="${this.escapeAttr(title)}">
            <span class="swatch" style="background:${this.escapeAttr(swatchColor)}"></span>
            <span class="label">${this.escapeHtml(item.name || alias)}</span>
          </button>
        `;
      })
      .join("");
    const paletteHtml = this._isOpen ? `<div class="palette">${options}</div>` : "";

    return `
      <div class="picker">
        <button class="primitive-colour" type="button" aria-expanded="${this._isOpen ? "true" : "false"}" title="${this.escapeAttr(selectedTitle)}">
          <span class="swatch" style="background:${this.escapeAttr(selectedColor)}"></span>
          <span class="label">${this.escapeHtml(selectedTitle)}</span>
          <span class="chevron">${this._isOpen ? "^" : "v"}</span>
        </button>
        ${paletteHtml}
      </div>
    `;
  }

  escapeHtml(value) {
    return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
  }

  escapeAttr(value) {
    return this.escapeHtml(value);
  }

  resolveCssColor(value) {
    const trimmed = String(value || "").trim();

    if (/^#(?:[0-9a-f]{3,4}|[0-9a-f]{6}|[0-9a-f]{8})$/i.test(trimmed) ||
        /^(?:rgb|rgba|hsl|hsla|oklch|oklab|lab|lch)\(/i.test(trimmed)) {
      return trimmed;
    }

    return "";
  }

  getItemTitle(item, selected) {
    const name = item.name || "";
    const alias = item.value || "";
    const label = name && alias && name.toLowerCase() !== alias.toLowerCase()
      ? `${name} (${alias})`
      : name || alias;

    return `${label}${selected ? " - selected" : ""}`;
  }

  getViewMode(items) {
    const selectableBaseItems = items.filter((item) => {
      const name = String(item.name || "").trim();
      const value = String(item.value || "").trim();
      return name && value && name.toLowerCase() === value.toLowerCase();
    });

    return selectableBaseItems.length === items.length && items.length > 0
      ? "list"
      : "grid";
  }

  groupItems(items) {
    const groups = new Map();

    items.forEach((item) => {
      const parsed = this.parseItemName(item.name || "");
      const key = parsed.family.toLowerCase();
      const group = groups.get(key) || { family: parsed.family, items: [] };
      group.items.push({ ...item, shade: parsed.shade });
      groups.set(key, group);
    });

    return Array.from(groups.values());
  }

  getShades(groups) {
    const preferred = ["50", "100", "200", "300", "400", "500", "600", "700", "800", "900", "950"];
    const seen = new Set();

    groups.forEach((group) => {
      group.items.forEach((item) => {
        if (item.shade) {
          seen.add(item.shade);
        }
      });
    });

    return preferred.filter((shade) => seen.has(shade));
  }

  parseItemName(name) {
    const trimmed = name.trim();
    const match = /^(.*?)[\s-]+(\d{2,3})$/.exec(trimmed);
    if (!match) {
      return { family: name, shade: "" };
    }

    return {
      family: this.toTitleCase(match[1].replaceAll("-", " ")),
      shade: match[2],
    };
  }

  toTitleCase(value) {
    return value.replace(/\b[a-z]/g, (letter) => letter.toUpperCase());
  }
}

customElements.define("my-contentment-color-swatches", MyContentmentColorSwatches);
