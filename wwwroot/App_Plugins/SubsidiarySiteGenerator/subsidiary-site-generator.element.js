import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";

const STEP_TEMPLATE = [
  ["validate-inputs", "Validate Inputs"],
  ["create-root-site", "Create Root Site"],
  ["create-home-page", "Create Home Page"],
  ["create-about-page", "Create About Page"],
  ["create-site-settings", "Create Site Settings"],
  ["create-identity-settings", "Create Identity Settings"],
  ["create-header-settings", "Create Header Settings"],
  ["create-footer-settings", "Create Footer Settings"],
  ["create-theme-settings", "Create Theme Settings"],
  ["create-site-theme-roles", "Create Site Theme Roles"],
  ["upload-and-assign-logos", "Upload And Assign Logos"],
  ["apply-domains-and-cultures", "Apply Domains And Cultures"],
  ["create-or-assign-users", "Create Or Assign Users"],
  ["publish-required-nodes", "Publish Required Nodes"],
  ["completion-summary", "Completion Summary"],
];

class SiteSubsidiarySiteGenerator extends UmbElementMixin(HTMLElement) {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.siteName = "";
    this.designTokenJson = "";
    this.logoFiles = [];
    this.isGenerating = false;
    this.isFetchingDefaults = false;
    this.isValidating = false;
    this.validation = { errors: [], warnings: [] };
    this.result = null;
    this.steps = this.createEmptySteps();
  }

  connectedCallback() {
    this.render();
  }

  createEmptySteps() {
    return STEP_TEMPLATE.map(([alias, name]) => ({
      alias,
      name,
      status: "Not started",
      messages: [],
      errors: [],
    }));
  }

  setStepState(stepAlias, status, messages = [], errors = []) {
    this.steps = this.steps.map((step) => (step.alias === stepAlias ? { ...step, status, messages: [...messages], errors: [...errors] } : step));
  }

  resetForRun() {
    this.result = null;
    this.validation = { errors: [], warnings: [] };
    this.steps = this.createEmptySteps();
    this.setStepState("validate-inputs", "In progress", ["Preparing request."]);
  }

  async handleGenerateDefaults() {
    this.isFetchingDefaults = true;
    this.render();

    try {
      const payload = await this.requestJson("/umbraco/management/api/v1/subsidiary-site-generator/default-theme");
      this.designTokenJson = payload.json || "";
      this.validation = { errors: [], warnings: [] };
    } catch (error) {
      this.validation = {
        errors: [this.describeError(error, "Failed to generate default tokens.")],
        warnings: [],
      };
    } finally {
      this.isFetchingDefaults = false;
      this.render();
    }
  }

  async handleValidateTheme() {
    this.designTokenJson = this.normalizeDesignTokenJson(this.designTokenJson);
    const localErrors = this.validateLocalInputs({ requireJson: true });
    if (localErrors.length) {
      this.validation = { errors: localErrors, warnings: [] };
      this.render();
      return;
    }

    this.isValidating = true;
    this.render();

    try {
      const payload = await this.requestJson("/umbraco/management/api/v1/subsidiary-site-generator/validate-theme", {
        method: "POST",
        body: {
          request: {
            designTokenJson: this.designTokenJson,
          },
        },
      });
      this.validation = {
        errors: Array.isArray(payload.errors) ? payload.errors : [],
        warnings: Array.isArray(payload.warnings) ? payload.warnings : [],
      };
    } catch (error) {
      this.validation = {
        errors: [this.describeError(error, "Theme validation failed.")],
        warnings: [],
      };
    } finally {
      this.isValidating = false;
      this.render();
    }
  }

  async handleGenerate() {
    this.designTokenJson = this.normalizeDesignTokenJson(this.designTokenJson);
    const localErrors = this.validateLocalInputs({ requireJson: false });
    if (localErrors.length) {
      this.validation = { errors: localErrors, warnings: [] };
      this.render();
      return;
    }

    this.resetForRun();
    this.isGenerating = true;
    this.render();

    try {
      const logoFiles = await Promise.all(this.logoFiles.map((file) => this.serializeLogoFile(file)));
      const payload = await this.requestJson("/umbraco/management/api/v1/subsidiary-site-generator/generate", {
        method: "POST",
        body: {
          request: {
            siteName: this.siteName,
            designTokenJson: this.designTokenJson,
            logoFiles,
          },
        },
      });

      this.result = payload;
      this.steps = Array.isArray(payload.steps) && payload.steps.length ? payload.steps : this.createEmptySteps();
      this.validation = {
        errors: [],
        warnings: Array.isArray(payload.warnings) ? payload.warnings : [],
      };
    } catch (error) {
      this.setStepState("validate-inputs", "Error", [], [this.describeError(error, "Generation failed.")]);
      this.validation = { errors: [this.describeError(error, "Generation failed.")], warnings: [] };
    } finally {
      this.isGenerating = false;
      this.render();
    }
  }

  handleSiteNameInput(value) {
    this.siteName = value;
  }

  handleDesignJsonInput(value) {
    this.designTokenJson = value;
  }

  async handleJsonFileChange(file) {
    if (!file) {
      return;
    }

    try {
      this.designTokenJson = this.normalizeDesignTokenJson(await file.text());
      this.validation = { errors: [], warnings: [] };
    } catch (error) {
      this.validation = {
        errors: [this.describeError(error, "Could not read JSON file.")],
        warnings: [],
      };
    }

    this.render();
  }

  handleLogoFileChange(fileList) {
    this.logoFiles = Array.from(fileList || []);
    this.render();
  }

  validateLocalInputs({ requireJson }) {
    const errors = [];
    if (!String(this.siteName || "").trim()) {
      errors.push("Site name is required.");
    }

    const rawJson = String(this.designTokenJson || "").trim();
    if (requireJson && !rawJson) {
      errors.push("Design token JSON is required for validation.");
    }

    if (rawJson) {
      try {
        const parsed = JSON.parse(rawJson);
        if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
          errors.push("Design token JSON must be an object.");
        }
      } catch {
        errors.push("Design token JSON is invalid.");
      }
    }

    return errors;
  }

  normalizeDesignTokenJson(rawValue) {
    const raw = String(rawValue || "").trim();
    if (!raw) {
      return "";
    }

    try {
      const parsed = JSON.parse(raw);

      if (typeof parsed === "string") {
        return parsed;
      }

      if (parsed && typeof parsed === "object" && !Array.isArray(parsed) && typeof parsed.json === "string" && Object.prototype.hasOwnProperty.call(parsed, "isValid")) {
        return parsed.json;
      }
    } catch {
      return rawValue;
    }

    return rawValue;
  }

  async serializeLogoFile(file) {
    const bytes = new Uint8Array(await file.arrayBuffer());
    let binary = "";

    for (const byte of bytes) {
      binary += String.fromCharCode(byte);
    }

    return {
      fileName: file.name,
      contentType: file.type || "application/octet-stream",
      base64: btoa(binary),
    };
  }

  describeError(error, fallbackMessage) {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    if (typeof error === "string" && error) {
      return error;
    }

    try {
      return JSON.stringify(error);
    } catch {
      return fallbackMessage;
    }
  }

  async requestJson(url, options = {}) {
    const method = String(options.method || "GET").toLowerCase();
    const headers = {
      ...(options.headers || {}),
    };
    const body = options.body;

    if (body && !headers["Content-Type"]) {
      headers["Content-Type"] = "application/json";
    }

    const clientMethod = typeof umbHttpClient[method] === "function" ? umbHttpClient[method].bind(umbHttpClient) : null;
    if (!clientMethod) {
      throw new Error(`Unsupported request method: ${method}`);
    }

    const response = await clientMethod({
      url,
      headers,
      body,
      security: [{ scheme: "bearer", type: "http" }],
    });

    if (response.error) {
      const payload = response.error?.body || response.error;
      const detail = payload?.detail || payload?.title || payload?.message || response.error?.statusText || "Request failed.";
      const raw = this.trySerialize(payload);
      throw new Error(raw ? `${detail} | ${raw}` : detail);
    }

    return response.data;
  }

  trySerialize(value) {
    try {
      return JSON.stringify(value);
    } catch {
      return "";
    }
  }

  renderValidation() {
    const issues = [...this.validation.errors, ...this.validation.warnings];
    if (!issues.length) {
      return "";
    }

    return `
      <div class="notice ${this.validation.errors.length ? "notice-error" : "notice-warn"}">
        <strong>${this.validation.errors.length ? "Validation issues" : "Validation warnings"}</strong>
        <ul>${issues.map((item) => `<li>${this.escapeHtml(item)}</li>`).join("")}</ul>
      </div>
    `;
  }

  renderSummary() {
    if (!this.result) {
      return "";
    }

    return `
      <section class="panel summary">
        <div class="panel-head">
          <div>
            <p class="eyebrow">Completion</p>
            <h2>${this.result.success ? "Site generated" : "Generation stopped"}</h2>
          </div>
          <span class="summary-badge ${this.result.success ? "status-complete" : "status-error"}">${this.escapeHtml(this.result.success ? "Success" : "Error")}</span>
        </div>
        <div class="summary-grid">
          <div>
            <span class="summary-label">Site</span>
            <span>${this.escapeHtml(this.result.newSiteName || "Not created")}</span>
          </div>
          <div>
            <span class="summary-label">Tenant Key</span>
            <span>${this.escapeHtml(this.result.tenantKey || "N/A")}</span>
          </div>
          <div>
            <span class="summary-label">Preview Domain</span>
            <span>${this.escapeHtml(this.result.previewDomain || "Not assigned")}</span>
          </div>
          <div>
            <span class="summary-label">Stylesheet</span>
            <span>${this.escapeHtml(this.result.stylesheetUrl || "Generated on content node only")}</span>
          </div>
        </div>
        ${this.renderSummaryList("Created pages/settings", this.result.createdItems)}
        ${this.renderSummaryList("Warnings", this.result.warnings)}
        ${this.renderSummaryList("User details to copy", this.result.userDetailsToCopy)}
        ${this.renderSummaryList("Manual actions", this.result.manualActions)}
      </section>
    `;
  }

  renderSummaryList(title, items) {
    if (!Array.isArray(items) || !items.length) {
      return "";
    }

    return `
      <div class="summary-list">
        <h3>${this.escapeHtml(title)}</h3>
        <ul>${items.map((item) => `<li>${this.escapeHtml(item)}</li>`).join("")}</ul>
      </div>
    `;
  }

  renderSteps() {
    return `
      <section class="panel">
        <div class="panel-head">
          <div>
            <p class="eyebrow">Execution</p>
            <h2>Generation steps</h2>
          </div>
          <span class="step-count">${this.steps.filter((step) => step.status === "Complete").length}/${this.steps.length}</span>
        </div>
        <div class="step-list">
          ${this.steps.map((step) => this.renderStep(step)).join("")}
        </div>
      </section>
    `;
  }

  renderStep(step) {
    const statusClass = this.getStatusClass(step.status);
    return `
      <article class="step-card">
        <div class="step-head">
          <div>
            <h3>${this.escapeHtml(step.name)}</h3>
          </div>
          <span class="status-pill ${statusClass}">${this.escapeHtml(step.status)}</span>
        </div>
        ${step.messages?.length ? `<ul class="step-messages">${step.messages.map((message) => `<li>${this.escapeHtml(message)}</li>`).join("")}</ul>` : ""}
        ${step.errors?.length ? `<ul class="step-errors">${step.errors.map((message) => `<li>${this.escapeHtml(message)}</li>`).join("")}</ul>` : ""}
      </article>
    `;
  }

  getStatusClass(status) {
    const value = String(status || "").toLowerCase();
    if (value === "complete") {
      return "status-complete";
    }

    if (value === "in progress") {
      return "status-progress";
    }

    if (value === "error") {
      return "status-error";
    }

    return "status-idle";
  }

  render() {
    if (!this.shadowRoot) {
      return;
    }

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: block;
          color: #152033;
          font-family: Verdana, Arial, sans-serif;
          line-height: 1.4;
        }

        * {
          box-sizing: border-box;
        }

        .shell {
          display: grid;
          gap: 20px;
          padding: 24px;
          background:
            radial-gradient(circle at top right, rgba(204, 251, 241, 0.9), transparent 30%),
            linear-gradient(180deg, #f7fafc 0%, #edf2f7 100%);
        }

        .hero {
          padding: 28px;
          border-radius: 20px;
          background: linear-gradient(135deg, #062c30 0%, #0f766e 52%, #7dd3fc 100%);
          color: #f8fafc;
          box-shadow: 0 18px 40px rgba(6, 44, 48, 0.18);
        }

        .hero h1,
        .panel h2,
        .step-card h3,
        .summary-list h3 {
          margin: 0;
          font-weight: 700;
        }

        .hero h1 {
          font-size: clamp(1.9rem, 2.5vw, 2.7rem);
        }

        .hero p {
          max-width: 70ch;
          margin: 10px 0 0;
          color: rgba(248, 250, 252, 0.9);
          line-height: 1.4;
        }

        .grid {
          display: grid;
          grid-template-columns: minmax(0, 1.1fr) minmax(320px, 0.9fr);
          gap: 20px;
          align-items: start;
        }

        .panel {
          padding: 22px;
          border: 1px solid rgba(15, 23, 42, 0.08);
          border-radius: 18px;
          background: rgba(255, 255, 255, 0.92);
          box-shadow: 0 16px 34px rgba(15, 23, 42, 0.06);
          backdrop-filter: blur(14px);
        }

        .panel-head {
          display: flex;
          justify-content: space-between;
          gap: 16px;
          align-items: start;
          margin-bottom: 18px;
        }

        .eyebrow {
          margin: 0 0 6px;
          font-size: 0.76rem;
          font-weight: 700;
          letter-spacing: 0.14em;
          text-transform: uppercase;
          color: #0f766e;
        }

        .lead {
          margin: 8px 0 0;
          color: #475569;
          line-height: 1.4;
        }

        .field-grid {
          display: grid;
          gap: 18px;
        }

        .field {
          display: grid;
          gap: 8px;
        }

        .field label {
          font-size: 0.92rem;
          font-weight: 700;
          color: #0f172a;
        }

        .field small {
          color: #64748b;
          line-height: 1.4;
        }

        input[type="text"],
        textarea,
        input[type="file"] {
          width: 100%;
          border: 1px solid #cbd5e1;
          border-radius: 14px;
          background: #fff;
          color: #0f172a;
          padding: 12px 14px;
          font: inherit;
        }
        input[type="text"] + p,
        textarea + p,
        input[type="file"] + p {
          margin-top: 0;
          line-height: 1.4;
        }

        textarea {
          min-height: 280px;
          resize: vertical;
          font-family: "Cascadia Code", "SFMono-Regular", Consolas, monospace;
          line-height: 1.5;
        }

        input[type="text"]:focus,
        textarea:focus,
        input[type="file"]:focus {
          outline: 2px solid rgba(13, 148, 136, 0.2);
          border-color: #0f766e;
        }

        .toolbar {
          display: flex;
          flex-wrap: wrap;
          gap: 10px;
        }

        button {
          border: 0;
          border-radius: 999px;
          padding: 11px 16px;
          font: inherit;
          font-weight: 700;
          cursor: pointer;
          transition: transform 120ms ease, opacity 120ms ease;
        }

        button:disabled {
          cursor: wait;
          opacity: 0.6;
        }

        button:hover:not(:disabled) {
          transform: translateY(-1px);
        }

        .button-primary {
          background: linear-gradient(135deg, #0f766e, #155e75);
          color: #f8fafc;
        }

        .button-secondary {
          background: #dff8f4;
          color: #115e59;
        }

        .button-subtle {
          background: #f1f5f9;
          color: #0f172a;
        }

        .logo-list {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin: 0;
          padding: 0;
          list-style: none;
        }

        .logo-list li {
          padding: 8px 12px;
          border-radius: 999px;
          background: #ecfeff;
          color: #155e75;
          font-size: 0.88rem;
          font-weight: 700;
        }

        .notice {
          margin-top: 14px;
          padding: 14px 16px;
          border-radius: 16px;
        }

        .notice strong {
          display: block;
          margin-bottom: 8px;
        }

        .notice ul,
        .summary-list ul,
        .step-messages,
        .step-errors {
          margin: 0;
          padding-left: 18px;
        }
          
        .notice ul li,
        .summary-list ul li,
        .step-messages li,
        .step-errors li {
          font-size: 16px;
          line-height: 1.4;
        }

        .notice-error {
          background: #fff1f2;
          color: #881337;
          border: 1px solid #fecdd3;
        }

        .notice-warn {
          background: #fff7ed;
          color: #9a3412;
          border: 1px solid #fed7aa;
        }

        .step-count,
        .summary-badge {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          min-width: 56px;
          padding: 8px 12px;
          border-radius: 999px;
          font-size: 0.88rem;
          font-weight: 700;
        }

        .step-list {
          display: grid;
          gap: 12px;
        }

        .step-card {
          padding: 16px;
          border-radius: 16px;
          border: 1px solid #e2e8f0;
          background: linear-gradient(180deg, rgba(248, 250, 252, 0.9), #fff);
        }

        .step-head {
          display: flex;
          justify-content: space-between;
          gap: 12px;
          align-items: center;
        }

        .step-head p {
          margin: 4px 0 0;
          color: #64748b;
          font-size: 0.85rem;
        }

        .status-pill,
        .summary-badge {
          white-space: nowrap;
        }

        .status-complete {
          background: #dcfce7;
          color: #166534;
        }

        .status-progress {
          background: #dbeafe;
          color: #1d4ed8;
        }

        .status-error {
          background: #fee2e2;
          color: #991b1b;
        }

        .status-idle {
          background: #e2e8f0;
          color: #334155;
        }

        .step-messages li {
          color: #334155;
        }

        .step-errors li {
          color: #991b1b;
        }

        .stack {
          display: grid;
          gap: 20px;
        }

        .summary-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 14px;
          margin-bottom: 16px;
        }

        .summary-grid div {
          display: grid;
          gap: 4px;
          padding: 12px 14px;
          border-radius: 14px;
          background: #f8fafc;
          border: 1px solid #e2e8f0;
        }

        .summary-label {
          color: #64748b;
          font-size: 0.8rem;
          font-weight: 700;
          letter-spacing: 0.05em;
          text-transform: uppercase;
        }

        .summary-list + .summary-list {
          margin-top: 16px;
        }

        @media (max-width: 1100px) {
          .grid {
            grid-template-columns: 1fr;
          }
        }

        @media (max-width: 720px) {
          .shell {
            padding: 16px;
          }

          .hero,
          .panel {
            padding: 18px;
          }

          .summary-grid {
            grid-template-columns: 1fr;
          }
        }
      </style>
      <div class="shell">
        <section class="hero">
          <h1>Generate Tenant Site</h1>
          <p>Creates the tenant root, starter pages, site settings tree, imported theme tokens, generated frontend theme output.</p>
        </section>
        <div class="grid">
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Inputs</p>
                <h2>Site setup</h2>
                <p class="lead">Site name must be unique. Theme JSON can be pasted, uploaded, or generated from defaults. Uploaded logo assets are reused for header, footer, and favicon where possible.</p>
              </div>
            </div>
            <div class="field-grid">
              <div class="field">
                <label for="site-name">Site Name</label>
                <input id="site-name" type="text" value="${this.escapeAttr(this.siteName)}" placeholder="Acme Subsidiary" />
                <p>Used for the tenant root node, media folder, and derived tenant key.</p>
              </div>
              <div class="field">
                <label for="logo-files">Logo Assets</label>
                <input id="logo-files" type="file" accept=".svg,.png,.jpg,.jpeg,.webp,.gif,.ico" multiple />
                <p>First upload becomes the default header logo. Second file becomes the footer logo. Files containing "favicon" or "icon" are preferred for favicon assignment.</p>
                ${this.logoFiles.length ? `<ul class="logo-list">${this.logoFiles.map((file) => `<li>${this.escapeHtml(file.name)}</li>`).join("")}</ul>` : ""}
              </div>
              <div class="field">
                <label for="theme-json-file">Theme JSON File</label>
                <input id="theme-json-file" type="file" accept=".json,application/json" />
                <p>Upload a DTCG-style JSON file to hydrate the textarea.</p>
              </div>
              <div class="field">
                <label for="theme-json">Design Tokens JSON</label>
                <textarea id="theme-json" spellcheck="false" placeholder="{&#10;  &quot;color&quot;: { ... }&#10;}">${this.escapeHtml(this.designTokenJson)}</textarea>
                <p>Generate Base Tokens writes default DTCG JSON here. Those values map into Theme Settings and Applied Theme Styles.</p>
              </div>
              <div class="toolbar">
                <button class="button-secondary" data-action="defaults" ${this.isFetchingDefaults ? "disabled" : ""}>${this.isFetchingDefaults ? "Generating defaults..." : "Generate Base Tokens"}</button>
                <button class="button-subtle" data-action="validate" ${this.isValidating ? "disabled" : ""}>${this.isValidating ? "Validating..." : "Validate Theme JSON"}</button>
                <button class="button-primary" data-action="generate" ${this.isGenerating ? "disabled" : ""}>${this.isGenerating ? "Generating Site..." : "Generate Site"}</button>
              </div>
              ${this.renderValidation()}
            </div>
          </section>
          <div class="stack">
            ${this.renderSteps()}
            ${this.renderSummary()}
          </div>
        </div>
      </div>
    `;

    this.shadowRoot.getElementById("site-name")?.addEventListener("input", (event) => {
      this.handleSiteNameInput(event.target.value);
    });

    this.shadowRoot.getElementById("theme-json")?.addEventListener("input", (event) => {
      this.handleDesignJsonInput(event.target.value);
    });

    this.shadowRoot.getElementById("theme-json-file")?.addEventListener("change", async (event) => {
      await this.handleJsonFileChange(event.target.files?.[0] || null);
    });

    this.shadowRoot.getElementById("logo-files")?.addEventListener("change", (event) => {
      this.handleLogoFileChange(event.target.files);
    });

    this.shadowRoot.querySelector('[data-action="defaults"]')?.addEventListener("click", () => {
      this.handleGenerateDefaults();
    });

    this.shadowRoot.querySelector('[data-action="validate"]')?.addEventListener("click", () => {
      this.handleValidateTheme();
    });

    this.shadowRoot.querySelector('[data-action="generate"]')?.addEventListener("click", () => {
      this.handleGenerate();
    });
  }

  escapeHtml(value) {
    return String(value ?? "")
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

customElements.define("site-subsidiary-site-generator", SiteSubsidiarySiteGenerator);
