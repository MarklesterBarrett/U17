import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

const OPTIONS = [
  { value: "sans", label: "Sans" },
  { value: "serif", label: "Serif" },
  { value: "mono", label: "Mono" },
  { value: "custom", label: "Custom - Specify" },
];

const LEGACY_PRESETS = {
  sans: "ui-sans-serif, system-ui, sans-serif",
  serif: 'ui-serif, Georgia, Cambria, "Times New Roman", Times, serif',
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace',
};

class MyFontFamilyValue extends HTMLElement {
  constructor() {
    super();
    this._value = { preset: "sans", customValue: "" };
    this.attachShadow({ mode: "open" });
  }

  set value(value) {
    this._value = this.parseValue(value);
    this.render();
  }

  get value() {
    const preset = this._value.preset || "sans";
    const customValue = (this._value.customValue || "").trim();

    if (preset === "custom") {
      return customValue
        ? JSON.stringify({ preset, customValue })
        : JSON.stringify({ preset, customValue: "" });
    }

    return JSON.stringify({ preset, customValue: "" });
  }

  connectedCallback() {
    this.render();
  }

  parseValue(value) {
    if (!value) {
      return { preset: "sans", customValue: "" };
    }

    if (typeof value === "object" && value !== null) {
      return this.normalizeValue(value);
    }

    if (typeof value === "string") {
      try {
        const parsed = JSON.parse(value);
        if (parsed && typeof parsed === "object") {
          return this.normalizeValue(parsed);
        }
      } catch {
        return this.parseLegacyString(value);
      }
    }

    return { preset: "sans", customValue: "" };
  }

  parseLegacyString(value) {
    const normalized = String(value).trim();

    if (!normalized) {
      return { preset: "sans", customValue: "" };
    }

    for (const [preset, presetValue] of Object.entries(LEGACY_PRESETS)) {
      if (normalized === presetValue) {
        return { preset, customValue: "" };
      }
    }

    return { preset: "custom", customValue: normalized };
  }

  normalizeValue(value) {
    const preset = OPTIONS.some((option) => option.value === value.preset)
      ? value.preset
      : "sans";

    return {
      preset,
      customValue: typeof value.customValue === "string" ? value.customValue : "",
    };
  }

  updatePreset(value) {
    this._value = {
      preset: value,
      customValue: value === "custom" ? this._value.customValue || "" : "",
    };

    this.dispatchEvent(new UmbChangeEvent());
    this.render();
  }

  updateCustomValue(value) {
    this._value = {
      ...this._value,
      customValue: value,
    };

    this.dispatchEvent(new UmbChangeEvent());
    this.render();
  }

  render() {
    if (!this.shadowRoot) {
      return;
    }

    const showCustom = this._value.preset === "custom";

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          max-width: 760px;
        }

        .font-family-value {
          display: grid;
          gap: 10px;
        }

        label {
          display: grid;
          gap: 6px;
          color: #1b264f;
          font: inherit;
          font-weight: 700;
        }

        select,
        input {
          box-sizing: border-box;
          width: 100%;
          min-height: 38px;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: inherit;
          font: inherit;
          padding: 8px 10px;
        }

        select:focus,
        input:focus {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
          outline: 0;
        }

        .hint {
          color: #5c6370;
          font-size: 0.92em;
          line-height: 1.4;
          margin: 0;
        }

        .example {
          font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
          font-size: 0.92em;
        }
      </style>
      <div class="font-family-value">
        <label>
          <span>Font source</span>
          <select data-element="preset">
            ${OPTIONS.map((option) => this.renderOption(option)).join("")}
          </select>
        </label>
        ${showCustom ? this.renderCustomField() : ""}
      </div>
    `;

    const preset = this.shadowRoot.querySelector('[data-element="preset"]');
    if (preset) {
      preset.addEventListener("change", (event) => {
        this.updatePreset(event.target.value);
      });
    }

    const custom = this.shadowRoot.querySelector('[data-element="custom"]');
    if (custom) {
      custom.addEventListener("input", (event) => {
        this.updateCustomValue(event.target.value);
      });
    }
  }

  renderOption(option) {
    const selected = option.value === this._value.preset ? ' selected="selected"' : "";
    return `<option value="${this.escapeAttr(option.value)}"${selected}>${this.escapeHtml(option.label)}</option>`;
  }

  renderCustomField() {
    return `
      <label>
        <span>Custom font-family value</span>
        <input
          type="text"
          data-element="custom"
          value="${this.escapeAttr(this._value.customValue || "")}"
          placeholder="Inter, system-ui, sans-serif"
          autocomplete="off">
      </label>
      <p class="hint">Enter a valid CSS <span class="example">font-family</span> value, for example <span class="example">Inter, system-ui, sans-serif</span>.</p>
    `;
  }

  escapeAttr(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;");
  }
}

customElements.define("my-font-family-value", MyFontFamilyValue);
