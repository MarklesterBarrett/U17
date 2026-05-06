import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

const fields = [
  {
    alias: "mobile",
    label: "Mobile",
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" role="img"><title>Mobile spacing value</title><rect width="14" height="20" x="5" y="2" rx="2" ry="2"></rect><path d="M12 18h.01"></path></svg>',
  },
  {
    alias: "tablet",
    label: "Tablet",
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" role="img"><title>Tablet spacing value</title><rect width="16" height="20" x="4" y="2" rx="2" ry="2"></rect><path d="M12 18h.01"></path></svg>',
  },
  {
    alias: "desktop",
    label: "Desktop",
    icon: '<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.75" viewBox="0 0 24 24" role="img"><title>Desktop spacing value</title><rect width="20" height="14" x="2" y="3" rx="2"></rect><path d="M8 21h8M12 17v4"></path></svg>',
  },
];

class MyResponsiveSpacingValue extends HTMLElement {
  constructor() {
    super();
    this._value = {};
    this._hasRendered = false;
    this.attachShadow({ mode: "open" });
  }

  set value(value) {
    this._value = this.parseValue(value);
    if (this._hasRendered) {
      this.syncInputs();
      return;
    }

    this.render();
  }

  get value() {
    const value = {
      mobile: this.toCssPixelValue(this._value.mobile),
      tablet: this.toCssPixelValue(this._value.tablet),
      desktop: this.toCssPixelValue(this._value.desktop),
    };

    return Object.values(value).some((fieldValue) => String(fieldValue).trim())
      ? JSON.stringify(value)
      : "";
  }

  connectedCallback() {
    this.render();
  }

  updateValue(alias, value) {
    const sanitizedValue = this.sanitizeNumericValue(value);

    this._value = {
      ...this._value,
      [alias]: sanitizedValue,
    };

    this.dispatchEvent(new UmbChangeEvent());
  }

  parseValue(value) {
    if (!value) {
      return {};
    }

    if (typeof value === "object") {
      return {
        mobile: this.sanitizeNumericValue(value.mobile),
        tablet: this.sanitizeNumericValue(value.tablet),
        desktop: this.sanitizeNumericValue(value.desktop),
      };
    }

    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === "object"
        ? {
            mobile: this.sanitizeNumericValue(parsed.mobile),
            tablet: this.sanitizeNumericValue(parsed.tablet),
            desktop: this.sanitizeNumericValue(parsed.desktop),
          }
        : {};
    } catch {
      return {};
    }
  }

  render() {
    if (!this.shadowRoot) {
      return;
    }

    if (this._hasRendered) {
      this.syncInputs();
      return;
    }

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          max-width: 760px;
        }

        .spacing-value {
          display: flex;
          flex-wrap: wrap;
          gap: 10px 12px;
          align-items: center;
        }

        label {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          color: #1b264f;
          font: inherit;
          font-weight: 700;
          white-space: nowrap;
        }

        svg {
          width: 18px;
          height: 18px;
          flex: 0 0 18px;
          color: #1b264f;
        }

        .sr-only {
          position: absolute;
          width: 1px;
          height: 1px;
          padding: 0;
          margin: -1px;
          overflow: hidden;
          clip: rect(0, 0, 0, 0);
          white-space: nowrap;
          border: 0;
        }

        input {
          box-sizing: border-box;
          width: 48px;
          min-height: 34px;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: inherit;
          font: inherit;
          padding: 6px 10px;
        }

        input:focus {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
          outline: 0;
        }

        .input-wrap {
          display: inline-flex;
          align-items: center;
          gap: 4px;
        }

        .suffix {
          color: #5c6370;
          font-size: 12px;
          line-height: 1;
        }
      </style>
      <div class="spacing-value">
        ${fields.map((field) => this.renderField(field)).join("")}
      </div>
    `;

    this.shadowRoot.querySelectorAll("input").forEach((input) => {
      input.addEventListener("input", () => {
        const sanitizedValue = this.sanitizeNumericValue(input.value);

        if (input.value !== sanitizedValue) {
          input.value = sanitizedValue;
        }

        this.updateValue(input.getAttribute("data-alias") || "", sanitizedValue);
      });
    });

    this._hasRendered = true;
    this.syncInputs();
  }

  syncInputs() {
    if (!this.shadowRoot) {
      return;
    }

    const activeElement = this.shadowRoot.activeElement;

    this.shadowRoot.querySelectorAll("input").forEach((input) => {
      if (input === activeElement) {
        return;
      }

      const alias = input.getAttribute("data-alias") || "";
      const nextValue = this._value[alias] || "";

      if (input.value !== nextValue) {
        input.value = nextValue;
      }
    });
  }

  renderField(field) {
    const value = this._value[field.alias] || "";

    return `
      <label>
        ${field.icon}
        <span class="sr-only">${field.label} *</span>
        <span class="input-wrap">
          <input
            type="text"
            inputmode="decimal"
            required
            data-alias="${field.alias}"
            value="${this.escapeAttr(value)}"
            autocomplete="off">
          ${field.alias === "desktop" ? '<span class="suffix">px</span>' : ""}
        </span>
      </label>
    `;
  }

  sanitizeNumericValue(value) {
    const input = String(value ?? "");
    let sawDecimalPoint = false;
    let result = "";

    for (const character of input) {
      if (character >= "0" && character <= "9") {
        result += character;
        continue;
      }

      if (character === "." && !sawDecimalPoint) {
        result += character;
        sawDecimalPoint = true;
      }
    }

    return result;
  }

  toCssPixelValue(value) {
    const normalized = this.sanitizeNumericValue(value);
    return normalized ? `${normalized}px` : "";
  }

  escapeAttr(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }
}

customElements.define("my-responsive-spacing-value", MyResponsiveSpacingValue);
