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
    this._config = {};
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
    const sharedUnit = this.getSharedUnit();
    const value = {
      mobile: this.toCssValue({ ...this._value.mobile, unit: sharedUnit }),
      tablet: this.toCssValue({ ...this._value.tablet, unit: sharedUnit }),
      desktop: this.toCssValue({ ...this._value.desktop, unit: sharedUnit }),
    };

    return Object.values(value).some((fieldValue) => String(fieldValue).trim())
      ? JSON.stringify({
          $type: "dimension",
          $value: value,
        })
      : "";
  }

  set config(value) {
    this._config = value && typeof value === "object" ? value : {};
    this._value = this.normalizeValueSet(this._value);

    if (this._hasRendered) {
      this.render();
    }
  }

  connectedCallback() {
    this.render();
  }

  updateValue(alias, nextValue) {
    if (alias === "shared-unit") {
      this._value = this.normalizeValueSet({
        mobile: { ...(this._value.mobile || this.createEmptyValue()), unit: nextValue.unit },
        tablet: { ...(this._value.tablet || this.createEmptyValue()), unit: nextValue.unit },
        desktop: { ...(this._value.desktop || this.createEmptyValue()), unit: nextValue.unit },
      });
      this.dispatchEvent(new UmbChangeEvent());
      return;
    }

    this._value = {
      ...this._value,
      [alias]: this.normalizeValue({
        ...(this._value[alias] || this.createEmptyValue()),
        ...nextValue,
      }),
    };

    this.dispatchEvent(new UmbChangeEvent());
  }

  parseValue(value) {
    if (!value) {
      return {};
    }

    if (typeof value === "object") {
      return this.parseValueObject(value);
    }

    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === "object"
        ? this.parseValueObject(parsed)
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

        .unit-wrap {
          display: inline-flex;
          align-items: center;
          gap: 4px;
        }

        select {
          box-sizing: border-box;
          min-height: 34px;
          border: 1px solid #d8d7d9;
          border-radius: 3px;
          background: #fff;
          color: inherit;
          font: inherit;
          padding: 6px 28px 6px 8px;
        }

        select:focus {
          border-color: #3879ff;
          box-shadow: 0 0 0 1px #3879ff;
          outline: 0;
        }
      </style>
      <div class="spacing-value">
        ${fields.map((field) => this.renderField(field)).join("")}
        <span class="unit-wrap">
          <select data-alias="shared-unit">
            ${this.getUnitOptions().map((option) => `<option value="${this.escapeAttr(option.value)}"${option.value === this.getSharedUnit() ? " selected" : ""}>${this.escapeHtml(option.label)}</option>`).join("")}
          </select>
        </span>
      </div>
    `;

    this.shadowRoot.querySelectorAll("input").forEach((input) => {
      input.addEventListener("input", () => {
        const sanitizedValue = this.sanitizeNumericValue(input.value);

        if (input.value !== sanitizedValue) {
          input.value = sanitizedValue;
        }

        this.updateValue(input.getAttribute("data-alias") || "", { number: sanitizedValue });
      });
    });

    this.shadowRoot.querySelectorAll("select").forEach((select) => {
      select.addEventListener("change", () => {
        this.updateValue(select.getAttribute("data-alias") || "", { unit: select.value });
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
      const nextValue = this._value[alias]?.number || "";

      if (input.value !== nextValue) {
        input.value = nextValue;
      }
    });

    this.shadowRoot.querySelectorAll('select[data-alias="shared-unit"]').forEach((select) => {
      if (select === activeElement) {
        return;
      }

      const nextValue = this.getSharedUnit();

      if (select.value !== nextValue) {
        select.value = nextValue;
      }
    });
  }

  renderField(field) {
    const value = this._value[field.alias] || this.createEmptyValue();

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
            value="${this.escapeAttr(value.number)}"
            autocomplete="off">
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

  createEmptyValue() {
    return { number: "", unit: this.getDefaultUnit() };
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
    const source = value?.$value && typeof value.$value === "object"
      ? value.$value
      : value;

    return {
      mobile: this.parseCssValue(source?.mobile),
      tablet: this.parseCssValue(source?.tablet),
      desktop: this.parseCssValue(source?.desktop),
    };
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

  normalizeValue(value) {
    return {
      number: this.sanitizeNumericValue(value?.number),
      unit: this.isAllowedUnit(value?.unit) ? value.unit : this.getDefaultUnit(),
    };
  }

  normalizeValueSet(value) {
    const sharedUnit = this.resolveSharedUnit(value);

    return {
      mobile: this.normalizeValue({ ...(value?.mobile ?? this.createEmptyValue()), unit: sharedUnit }),
      tablet: this.normalizeValue({ ...(value?.tablet ?? this.createEmptyValue()), unit: sharedUnit }),
      desktop: this.normalizeValue({ ...(value?.desktop ?? this.createEmptyValue()), unit: sharedUnit }),
    };
  }

  resolveSharedUnit(value) {
    const candidates = [
      value?.mobile?.unit,
      value?.tablet?.unit,
      value?.desktop?.unit,
    ];

    for (const candidate of candidates) {
      if (this.isAllowedUnit(candidate)) {
        return candidate;
      }
    }

    return this.getDefaultUnit();
  }

  getSharedUnit() {
    return this.resolveSharedUnit(this._value);
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
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }
}

customElements.define("my-responsive-spacing-value", MyResponsiveSpacingValue);
