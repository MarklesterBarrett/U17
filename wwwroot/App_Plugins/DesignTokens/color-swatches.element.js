class MyContentmentColorSwatches extends HTMLElement {
  constructor() {
    super();
    this._value = "";
    this._config = {};
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

  selectValue(nextValue) {
    this._value = nextValue;

    this.dispatchEvent(
      new CustomEvent("change", {
        bubbles: true,
        composed: true,
      }),
    );

    this.render();
  }

  render() {
    const items = this.items;

    const style = `
      <style>
        :host {
          display: block;
        }

        .grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(64px, 1fr));
          gap: 8px;
        }

        button.swatch {
          display: grid;

          gap: 8px;
          align-items: center;
          width: 100%;
          padding: 8px ;
          border: 1px solid #d9d9d9;
          border-radius: 8px;
          background: #fff;
          cursor: pointer;
              font: inherit;
        }

        .name {
          line-height: 1;
          word-break: break-word;
        }

        button.swatch.selected {
    border: 2px solid var(--uui-color-selected, #3544b1);
    border-radius: calc(var(--uui-border-radius, 3px) + 2px);
    box-shadow: 0 0 4px 0 var(--uui-color-selected, #3544b1), inset 0 0 2px 0 var(--uui-color-selected, #3544b1);
        }

        button.swatch.selected .name {
            font-weight: 600;
        }

        .chip {
          width: 100%;aspect-ratio: 1;
          border-radius: 4px;
          border: 1px solid rgba(0, 0, 0, 0.15);
          box-sizing: border-box;
        }



        .alias {
          line-height: 1;
          font-size: 11px;
          opacity: 0.75;
        }

      </style>
    `;

    const html = items
      .map((item) => {
        const alias = item.value || "";
        const selected = alias === this._value ? "selected" : "";
        const swatchColor = item.description || "transparent";
        const name = item.name || "";
        const title = `${name}${alias ? ` (${alias})` : ""}${selected ? " - selected" : ""}`;

        return `
        <button class="swatch ${selected}" type="button" data-value="${this.escapeAttr(alias)}" 
          aria-label="${title}" title="${title}">
          <span class="chip" style="background:${this.escapeAttr(swatchColor)}"></span>
          <span class="name">${this.escapeHtml(name)}</span>
        </button>
      `;
      })
      .join("");

    this.shadowRoot.innerHTML = `${style}<div class="grid">${html}</div>`;

    this.shadowRoot.querySelectorAll("button.swatch").forEach((btn) => {
      btn.addEventListener("click", () => {
        this.selectValue(btn.getAttribute("data-value") || "");
      });
    });
  }

  escapeHtml(value) {
    return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
  }

  escapeAttr(value) {
    return this.escapeHtml(value);
  }
}

customElements.define("my-contentment-color-swatches", MyContentmentColorSwatches);
