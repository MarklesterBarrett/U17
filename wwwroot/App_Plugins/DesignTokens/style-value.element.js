import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

class MyStyleValue extends HTMLElement {
  constructor() {
    super();
    this._value = "";
    this._hasRendered = false;
    this.attachShadow({ mode: "open" });
  }

  set value(value) {
    this._value = this.parseValue(value);

    if (this._hasRendered) {
      this.syncInput();
      return;
    }

    this.render();
  }

  get value() {
    return this._value ? `${this._value}px` : "";
  }

  connectedCallback() {
    this.render();
  }

  parseValue(value) {
    return this.sanitizeNumericValue(value);
  }

  updateValue(value) {
    this._value = this.sanitizeNumericValue(value);
    this.dispatchEvent(new UmbChangeEvent());
  }

  render() {
    if (!this.shadowRoot) {
      return;
    }

    if (this._hasRendered) {
      this.syncInput();
      return;
    }

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          max-width: 760px;
        }

        .style-value {
          display: inline-flex;
          align-items: center;
          gap: 6px;
        }

        input {
          box-sizing: border-box;
          width: 64px;
          min-height: 38px;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: inherit;
          font: inherit;
          padding: 8px 10px;
        }

        input:focus {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
          outline: 0;
        }

        .suffix {
          color: #5c6370;
          font-size: 12px;
          line-height: 1;
        }
      </style>
      <label class="style-value">
        <input type="text" inputmode="decimal" autocomplete="off">
        <span class="suffix">px</span>
      </label>
    `;

    const input = this.shadowRoot.querySelector("input");
    input?.addEventListener("input", () => {
      const sanitizedValue = this.sanitizeNumericValue(input.value);

      if (input.value !== sanitizedValue) {
        input.value = sanitizedValue;
      }

      this.updateValue(sanitizedValue);
    });

    this._hasRendered = true;
    this.syncInput();
  }

  syncInput() {
    const input = this.shadowRoot?.querySelector("input");

    if (!input || input === this.shadowRoot.activeElement) {
      return;
    }

    if (input.value !== this._value) {
      input.value = this._value;
    }
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
}

customElements.define("my-style-value", MyStyleValue);
