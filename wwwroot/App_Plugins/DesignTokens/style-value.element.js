import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

const UNIT_OPTIONS = [
  { value: "px", label: "px" },
  { value: "rem", label: "rem" },
  { value: "em", label: "em" },
  { value: "%", label: "%" },
  { value: "vw", label: "vw" },
  { value: "vh", label: "vh" },
  { value: "ch", label: "ch" },
  { value: "", label: "" },
];
const DEFAULT_UNIT_OPTIONS = UNIT_OPTIONS.filter((option) => option.value !== "");

class MyStyleValue extends HTMLElement {
  constructor() {
    super();
    this._value = { number: "", unit: "px" };
    this._config = {};
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
    const cssValue = this.toCssValue(this._value);
    return cssValue
      ? JSON.stringify({
          $type: "dimension",
          $value: cssValue,
        })
      : "";
  }

  set config(value) {
    this._config = value && typeof value === "object" ? value : {};
    this._value = this.normalizeValue(this._value);

    if (this._hasRendered) {
      this.render();
    }
  }

  connectedCallback() {
    this.render();
  }

  parseValue(value) {
    if (value && typeof value === "object") {
      return this.parseValueObject(value);
    }

    const input = String(value ?? "").trim();

    if (!input) {
      return this.createEmptyValue();
    }

    try {
      const parsed = JSON.parse(input);
      return parsed && typeof parsed === "object"
        ? this.parseValueObject(parsed)
        : this.parseCssValue(input);
    } catch {
      return this.parseCssValue(input);
    }
  }

  updateValue(nextValue) {
    this._value = this.normalizeValue({
      ...this._value,
      ...nextValue,
    });

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

        select {
          box-sizing: border-box;
          min-height: 38px;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: inherit;
          font: inherit;
          padding: 8px 30px 8px 10px;
        }

        select:focus {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
          outline: 0;
        }
      </style>
      <label class="style-value">
        <input type="text" inputmode="decimal" autocomplete="off">
        <select>
          ${this.getUnitOptions().map((option) => `<option value="${this.escapeAttr(option.value)}">${this.escapeHtml(option.label)}</option>`).join("")}
        </select>
      </label>
    `;

    const input = this.shadowRoot.querySelector("input");
    const select = this.shadowRoot.querySelector("select");

    input?.addEventListener("input", () => {
      const sanitizedValue = this.sanitizeNumericValue(input.value);

      if (input.value !== sanitizedValue) {
        input.value = sanitizedValue;
      }

      this.updateValue({ number: sanitizedValue });
    });

    select?.addEventListener("change", () => {
      this.updateValue({ unit: select.value });
    });

    this._hasRendered = true;
    this.syncInput();
  }

  syncInput() {
    const input = this.shadowRoot?.querySelector("input");
    const select = this.shadowRoot?.querySelector("select");

    if (!input || input === this.shadowRoot.activeElement) {
      if (select && select !== this.shadowRoot.activeElement && select.value !== this._value.unit) {
        select.value = this._value.unit;
      }

      return;
    }

    if (input.value !== this._value.number) {
      input.value = this._value.number;
    }

    if (select && select !== this.shadowRoot.activeElement && select.value !== this._value.unit) {
      select.value = this._value.unit;
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

  parseCssValue(value) {
    const input = String(value ?? "").trim();

    if (!input) {
      return this.createEmptyValue();
    }

    const match = input.match(/^([0-9]*\.?[0-9]+)\s*([a-z%]*)$/i);

    if (!match) {
      return this.normalizeValue({
        number: this.sanitizeNumericValue(input),
        unit: this.getDefaultUnit(),
      });
    }

    const unit = (match[2] || "").toLowerCase();

    return this.normalizeValue({
      number: this.sanitizeNumericValue(match[1]),
      unit,
    });
  }

  parseValueObject(value) {
    const cssValue = typeof value?.$value === "string"
      ? value.$value
      : typeof value?.value === "string"
        ? value.value
        : "";

    return this.parseCssValue(cssValue);
  }

  toCssValue(value) {
    const number = this.sanitizeNumericValue(value?.number);

    if (!number) {
      return "";
    }

    const unit = this.isAllowedUnit(value?.unit)
      ? value.unit
      : this.getDefaultUnit();

    return `${number}${unit}`;
  }

  createEmptyValue() {
    return { number: "", unit: this.getDefaultUnit() };
  }

  normalizeValue(value) {
    return {
      number: this.sanitizeNumericValue(value?.number),
      unit: this.isAllowedUnit(value?.unit) ? value.unit : this.getDefaultUnit(),
    };
  }

  getUnitOptions() {
    const configuredUnits = Array.isArray(this._config?.units)
      ? this._config.units
      : [];

    const configuredValues = configuredUnits
      .map((unit) => String(unit ?? "").trim().toLowerCase())
      .filter((unit) => UNIT_OPTIONS.some((option) => option.value === unit));

    if (configuredValues.length === 0) {
      return DEFAULT_UNIT_OPTIONS;
    }

    return configuredValues
      .map((value) => UNIT_OPTIONS.find((option) => option.value === value))
      .filter(Boolean);
  }

  getDefaultUnit() {
    const configuredDefault = String(this._config?.defaultUnit ?? "").trim().toLowerCase();
    return this.isAllowedUnit(configuredDefault)
      ? configuredDefault
      : this.getUnitOptions()[0]?.value ?? "px";
  }

  isAllowedUnit(unit) {
    return this.getUnitOptions().some((option) => option.value === unit);
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
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }
}

customElements.define("my-style-value", MyStyleValue);
