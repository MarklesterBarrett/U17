import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";

const PICKER_CONTEXT_OPTIONS = [
  { value: "auto", label: "Auto detect" },
  { value: "color", label: "Color" },
  { value: "dimension", label: "Dimension" },
  { value: "fontFamily", label: "Font family" },
  { value: "fontWeight", label: "Font weight" },
  { value: "duration", label: "Duration" },
  { value: "number", label: "Number" },
  { value: "typography.fontFamily", label: "Typography fontFamily" },
  { value: "typography.fontWeight", label: "Typography fontWeight" },
  { value: "typography.fontSize", label: "Typography fontSize" },
  { value: "typography.lineHeight", label: "Typography lineHeight" },
  { value: "typography.letterSpacing", label: "Typography letterSpacing" },
  { value: "border.width", label: "Border width" },
  { value: "border.color", label: "Border color" },
  { value: "shadow.color", label: "Shadow color" },
  { value: "shadow.offsetX", label: "Shadow offsetX" },
  { value: "shadow.offsetY", label: "Shadow offsetY" },
  { value: "shadow.blur", label: "Shadow blur" },
  { value: "shadow.spread", label: "Shadow spread" },
];

const PICKER_TOKEN_TYPE_OPTIONS = [
  { value: "", label: "All token types" },
  { value: "color", label: "Color" },
  { value: "dimension", label: "Dimension" },
  { value: "fontfamily", label: "Font family" },
  { value: "fontweight", label: "Font weight" },
  { value: "duration", label: "Duration" },
  { value: "number", label: "Number" },
];

const ALLOWED_THEME_VARIANTS = [
  { alias: "default", label: "Default", selector: ":root", isDefault: true },
  { alias: "dark", label: "Dark", selector: "[data-theme=\"dark\"]", isDefault: false },
  { alias: "highContrast", label: "High Contrast", selector: "[data-theme=\"high-contrast\"]", isDefault: false },
  { alias: "ocean", label: "Ocean", selector: "[data-theme=\"ocean\"]", isDefault: false },
];

const BASE_THEME_SECTIONS = [
  { id: "colours", label: "Colours", prefixes: ["color."] },
  { id: "typography", label: "Typography", prefixes: ["font.", "typography."] },
  { id: "spacing", label: "Spacing", prefixes: ["space."] },
  { id: "layout", label: "Layout", prefixes: ["layout."] },
  { id: "radius", label: "Radius", prefixes: ["radius."] },
  { id: "shadows", label: "Shadows", prefixes: ["shadow."] },
];

const SECTION_EMPTY_STATES = {
  colours: {
    title: "No colour values yet.",
    message: "Add a colour value to build surfaces, text, and actions.",
  },
  spacing: {
    title: "No spacing values yet.",
    message: "Add spacing values to keep layout rhythm consistent.",
  },
  layout: {
    title: "No layout values yet.",
    message: "Add layout values to control widths and gutters.",
  },
  typography: {
    title: "No typography values yet.",
    message: "Add typography values to shape headings, body copy, and labels.",
  },
  radius: {
    title: "No radius values yet.",
    message: "Add corner radius values to reuse across buttons, cards, and inputs.",
  },
  shadows: {
    title: "No shadow values yet.",
    message: "Add a shadow value to use across cards and panels.",
  },
};

const SEMANTIC_ROLE_GROUPS = [
  {
    id: "page",
    label: "Page",
    roles: [
      { path: "semantic.surface.page.background", label: "Page background", type: "color" },
      { path: "semantic.border.default", label: "Page border", type: "color" },
    ],
  },
  {
    id: "text",
    label: "Text",
    roles: [
      { path: "semantic.text.default", label: "Default text", type: "color" },
      { path: "semantic.text.muted", label: "Muted text", type: "color" },
      { path: "semantic.link.default", label: "Link", type: "color" },
    ],
  },
  {
    id: "buttons",
    label: "Buttons",
    roles: [
      { path: "semantic.action.primary.background", label: "Primary background", type: "color" },
      { path: "semantic.action.primary.text", label: "Primary text", type: "color" },
      { path: "semantic.action.secondary.background", label: "Secondary background", type: "color" },
      { path: "semantic.action.secondary.text", label: "Secondary text", type: "color" },
      { path: "semantic.action.secondary.border", label: "Secondary border", type: "color" },
    ],
  },
  {
    id: "cards",
    label: "Cards",
    roles: [
      { path: "semantic.surface.card.background", label: "Card background", type: "color" },
      { path: "component.card.radius", label: "Card radius", type: "dimension" },
      { path: "component.card.shadow", label: "Card shadow", type: "shadow" },
      { path: "semantic.border.strong", label: "Card border", type: "color" },
    ],
  },
  {
    id: "forms",
    label: "Forms",
    roles: [
      { path: "semantic.surface.input.background", label: "Input background", type: "color" },
      { path: "semantic.input.text", label: "Input text", type: "color" },
      { path: "semantic.input.border", label: "Input border", type: "color" },
      { path: "semantic.input.focusRing", label: "Focus ring", type: "color" },
    ],
  },
  {
    id: "feedback",
    label: "Feedback",
    roles: [
      { path: "semantic.action.primary.background", label: "Alert accent", type: "color" },
      { path: "semantic.text.default", label: "Alert text", type: "color" },
    ],
  },
];

class SiteDesignTokenManager extends UmbElementMixin(HTMLElement) {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.status = null;
    this.active = null;
    this.draft = null;
    this.tokens = [];
    this.selectedToken = null;
    this.editorName = "";
    this.editorJson = "";
    this.lastSavedDocumentId = "";
    this.validation = null;
    this.notice = "";
    this.activeTab = "base";
    this.previewTimer = null;
    this.previewRequestId = 0;
    this.loading = {
      init: false,
      validate: false,
      save: false,
      activate: false,
      rebuild: false,
      export: false,
      token: false,
      picker: false,
      preview: false,
    };
    this.picker = {
      open: false,
      query: "",
      tokenType: "",
      contextMode: "auto",
      detectedContext: "",
      appliedContext: "",
      appliedTokenType: "",
      items: [],
      emptyMessage: "",
      hasActiveBuild: true,
      selectionStart: 0,
      selectionEnd: 0,
    };
    this.preview = {
      css: "",
      scopedCss: "",
      response: null,
      theme: "default",
      updatedFromDraft: false,
    };
    this.tokenFilters = {
      sourceType: "all",
      flag: "all",
    };
    this.mergeLogOpen = false;
    this.starterInitAttempted = false;
    this.foundation = null;
    this.foundationSidebar = {
      open: false,
      mode: "",
      sectionId: "",
      tokenId: "",
      draftToken: null,
    };
  }

  connectedCallback() {
    this.loadDashboard();
  }

  async loadDashboard() {
    this.loading.init = true;
    this.render();

    try {
      const [status, tokens, foundation] = await Promise.all([
        this.requestJson("/umbraco/management/api/v1/design-tokens/status"),
        this.requestJson("/umbraco/management/api/v1/design-tokens/tokens").catch(() => []),
        this.requestJson("/umbraco/management/api/v1/design-tokens/foundation").catch(() => null),
      ]);

      const active = status?.activeDocument || null;
      const draft = status?.draftDocument || null;
      const draftHasValues = this.documentHasThemeValues(draft?.json);
      const foundationHasValues = this.foundationHasValues(foundation);

      if (!active && !draftHasValues && !foundationHasValues && !this.starterInitAttempted) {
        this.starterInitAttempted = true;
        await this.initializeStarterTheme();
        return;
      }

      this.status = status;
      this.active = active;
      this.draft = draft;
      this.tokens = Array.isArray(tokens) ? tokens : [];
      this.foundation = foundation || {
        sections: this.createEmptyFoundationSections(),
        paletteOptions: [],
        summary: this.summarizeFoundationSections(this.createEmptyFoundationSections()),
      };

      const currentDocument = draft || active;
      if (!this.editorJson && currentDocument?.json) {
        this.editorJson = currentDocument.json;
        this.editorName = currentDocument.name || "";
        this.lastSavedDocumentId = currentDocument.id || "";
      }
    } catch (error) {
      this.notice = this.describeError(error, "Could not load theme workbench.");
    } finally {
      this.loading.init = false;
      this.render();
    }
  }

  documentHasThemeValues(json) {
    if (!String(json || "").trim()) {
      return false;
    }

    try {
      return this.collectDraftTokens(JSON.parse(json)).length > 0;
    } catch {
      return false;
    }
  }

  foundationHasValues(foundation) {
    const summary = foundation?.summary;
    if (!summary || typeof summary !== "object") {
      return false;
    }

    return Object.values(summary).some((value) => Number(value) > 0);
  }

  async initializeStarterTheme() {
    try {
      const starterJson = await this.requestText("/umbraco/management/api/v1/design-tokens/starter");
      const response = await this.requestJson("/umbraco/management/api/v1/design-tokens/save-draft", {
        method: "POST",
        body: {
          name: "Starter theme",
          json: starterJson,
        },
      });

      this.editorJson = starterJson;
      this.editorName = response.document?.name || "Starter theme";
      this.lastSavedDocumentId = response.document?.id || "";
      this.validation = response;
      this.preview.response = null;
      this.preview.css = "";
      this.preview.scopedCss = "";
      this.preview.updatedFromDraft = false;
      this.notice = response.success
        ? "Draft theme ready. Preview your changes, then publish when ready."
        : "Starter theme loaded with issues. Review the draft before publishing.";

      await this.handlePreview({ quiet: true });
      await this.loadDashboard();
    } catch (error) {
      this.notice = this.describeError(error, "Could not load the starter theme.");
    }
  }

  async handleValidate() {
    this.loading.validate = true;
    this.notice = "";
    this.render();

    try {
      this.validation = await this.requestJson("/umbraco/management/api/v1/design-tokens/foundation/validate", {
        method: "POST",
        body: this.buildFoundationRequest(),
      });
      this.notice = this.validation.success ? "" : "Validation found blocking issues.";
    } catch (error) {
      this.notice = this.describeError(error, "Validation failed.");
    } finally {
      this.loading.validate = false;
      this.render();
    }
  }

  async handlePreview(options = {}) {
    const quiet = options.quiet === true;
    const requestId = ++this.previewRequestId;

    this.loading.preview = true;
    if (!quiet) {
      this.notice = "";
    }
    this.render();

    try {
      const response = await this.requestJson("/umbraco/management/api/v1/design-tokens/foundation/preview", {
        method: "POST",
        body: this.buildFoundationRequest(),
      });

      if (requestId !== this.previewRequestId) {
        return;
      }

      this.preview.response = response;
      this.validation = response;
      this.preview.updatedFromDraft = true;

      const previewThemes = Array.isArray(response.themeVariants) ? response.themeVariants.filter((item) => item.enabled) : [];
      if (!previewThemes.some((item) => item.alias === this.preview.theme)) {
        this.preview.theme = previewThemes.find((item) => item.isDefault)?.alias || previewThemes[0]?.alias || "default";
      }

      if (response.success) {
        this.preview.css = response.previewCss || "";
        this.preview.scopedCss = this.scopePreviewCss(this.preview.css);
        if (!quiet) {
          this.notice = "";
        }
      } else if (!quiet) {
        this.notice = "Preview build failed. Live site unchanged.";
      }
    } catch (error) {
      if (!quiet) {
        this.notice = this.describeError(error, "Preview build failed.");
      }
    } finally {
      if (requestId === this.previewRequestId) {
        this.loading.preview = false;
        this.render();
      }
    }
  }

  async handleSaveDraft() {
    this.loading.save = true;
    this.notice = "";
    this.render();

    try {
      const response = await this.requestJson("/umbraco/management/api/v1/design-tokens/foundation/save-draft", {
        method: "POST",
        body: this.buildFoundationRequest(),
      });

      this.validation = response;
      this.lastSavedDocumentId = response.document?.id || "";
      this.notice = response.success ? "" : "Draft saved with blocking issues.";
      await this.loadDashboard();
    } catch (error) {
      this.notice = this.describeError(error, "Draft save failed.");
      this.loading.save = false;
      this.render();
    }
  }

  async handleActivate() {
    this.notice = "";

    if (!this.lastSavedDocumentId) {
      await this.handleSaveDraft();
      if (!this.lastSavedDocumentId) {
        return;
      }
    }

    this.loading.activate = true;
    this.render();

    try {
      const response = await this.requestJson("/umbraco/management/api/v1/design-tokens/foundation/publish", {
        method: "POST",
        body: this.buildFoundationRequest(),
      });

      this.validation = response;
      this.notice = response.success ? "Theme published." : "Publish blocked.";
      await this.loadDashboard();
    } catch (error) {
      this.notice = this.describeError(error, "Publish failed.");
      this.loading.activate = false;
      this.render();
    }
  }

  async handleRebuild() {
    this.loading.rebuild = true;
    this.notice = "";
    this.render();

    try {
      this.validation = await this.requestJson("/umbraco/management/api/v1/design-tokens/rebuild", { method: "POST" });
      this.notice = this.validation.success ? "" : "Rebuild failed.";
      await this.loadDashboard();
    } catch (error) {
      this.notice = this.describeError(error, "Rebuild failed.");
      this.loading.rebuild = false;
      this.render();
    }
  }

  async handleExport() {
    this.loading.export = true;
    this.notice = "";
    this.render();

    try {
      const json = await this.requestText("/umbraco/management/api/v1/design-tokens/export");
      this.downloadText(`design-tokens-${this.createDateStamp()}.json`, json, "application/json");
    } catch (error) {
      this.notice = this.describeError(error, "Export failed.");
    } finally {
      this.loading.export = false;
      this.render();
    }
  }

  async handleImportFile(file) {
    if (!file) {
      return;
    }

    try {
      this.editorJson = await file.text();
      if (!this.editorName) {
        this.editorName = file.name.replace(/\.json$/i, "");
      }
      this.validation = null;
      this.preview.response = null;
      this.preview.css = "";
      this.preview.scopedCss = "";
      this.notice = "JSON imported into draft.";
    } catch (error) {
      this.notice = this.describeError(error, "Could not read imported JSON file.");
    }

    this.render();
  }

  async handleTokenSelect(path) {
    this.loading.token = true;
    this.render();

    try {
      this.selectedToken = await this.requestJson(`/umbraco/management/api/v1/design-tokens/tokens/${encodeURIComponent(path)}`);
    } catch (error) {
      this.notice = this.describeError(error, "Could not load token details.");
    } finally {
      this.loading.token = false;
      this.render();
    }
  }

  async openPicker() {
    const textarea = this.shadowRoot?.getElementById("doc-json");
    if (!textarea) {
      return;
    }

    this.picker.open = true;
    this.picker.query = "";
    this.picker.selectionStart = textarea.selectionStart || 0;
    this.picker.selectionEnd = textarea.selectionEnd || 0;
    this.picker.detectedContext = this.detectJsonFieldContext(textarea.value, this.picker.selectionStart);
    this.picker.items = [];
    this.picker.emptyMessage = "";
    await this.loadPicker();
  }

  closePicker() {
    this.picker.open = false;
    this.render();
  }

  async loadPicker() {
    this.loading.picker = true;
    this.notice = "";
    this.render();

    try {
      const params = new URLSearchParams();
      if (this.picker.query) {
        params.set("query", this.picker.query);
      }

      if (this.picker.tokenType) {
        params.set("tokenType", this.picker.tokenType);
      }

      const appliedContext = this.getActivePickerContext();
      if (appliedContext) {
        params.set("context", appliedContext);
      }

      params.set("limit", "200");

      const response = await this.requestJson(`/umbraco/management/api/v1/design-tokens/picker/search?${params.toString()}`);
      this.picker.items = Array.isArray(response.items) ? response.items : [];
      this.picker.emptyMessage = response.emptyMessage || "";
      this.picker.hasActiveBuild = response.hasActiveBuild !== false;
      this.picker.appliedContext = response.appliedContext || "";
      this.picker.appliedTokenType = response.appliedTokenType || "";
    } catch (error) {
      this.notice = this.describeError(error, "Could not load token picker.");
    } finally {
      this.loading.picker = false;
      this.render();
    }
  }

  async insertPickerItem(referenceValue) {
    const textarea = this.shadowRoot?.getElementById("doc-json");
    if (!textarea) {
      return;
    }

    const start = Number.isInteger(this.picker.selectionStart) ? this.picker.selectionStart : textarea.value.length;
    const end = Number.isInteger(this.picker.selectionEnd) ? this.picker.selectionEnd : start;
    this.editorJson = `${this.editorJson.slice(0, start)}${referenceValue}${this.editorJson.slice(end)}`;
    this.closePicker();
    this.notice = `Inserted ${referenceValue}.`;
    this.render();

    const nextTextarea = this.shadowRoot?.getElementById("doc-json");
    if (nextTextarea) {
      const position = start + referenceValue.length;
      nextTextarea.focus();
      nextTextarea.setSelectionRange(position, position);
    }
  }

  queuePreview() {
    clearTimeout(this.previewTimer);
    this.previewTimer = setTimeout(() => {
      if (this.tryParseEditorJson() || this.foundation?.sections) {
        this.handlePreview({ quiet: true });
      }
    }, 500);
  }

  buildFoundationRequest() {
    return {
      name: this.editorName,
      json: this.editorJson,
      sections: this.foundation?.sections || this.createEmptyFoundationSections(),
    };
  }

  createEmptyFoundationSections() {
    return {
      colours: [],
      typography: [],
      spacing: [],
      layout: [],
      radius: [],
      shadows: [],
    };
  }

  summarizeFoundationSections(sections) {
    return {
      colours: Array.isArray(sections?.colours) ? sections.colours.length : 0,
      typography: Array.isArray(sections?.typography) ? sections.typography.length : 0,
      spacing: Array.isArray(sections?.spacing) ? sections.spacing.length : 0,
      layout: Array.isArray(sections?.layout) ? sections.layout.length : 0,
      radius: Array.isArray(sections?.radius) ? sections.radius.length : 0,
      shadows: Array.isArray(sections?.shadows) ? sections.shadows.length : 0,
    };
  }

  cloneFoundation() {
    return JSON.parse(JSON.stringify(this.foundation || {
      sections: this.createEmptyFoundationSections(),
      paletteOptions: [],
      summary: this.summarizeFoundationSections(this.createEmptyFoundationSections()),
    }));
  }

  updateFoundation(mutator, successMessage) {
    const next = this.cloneFoundation();
    mutator(next);
    next.summary = this.summarizeFoundationSections(next.sections || this.createEmptyFoundationSections());
    this.foundation = next;
    if (successMessage) {
      this.notice = successMessage;
    }
    this.render();
    this.queuePreview();
  }

  getFoundationSectionItems(sectionId) {
    return this.foundation?.sections?.[sectionId] || [];
  }

  findFoundationToken(sectionId, tokenId) {
    return this.getFoundationSectionItems(sectionId).find((item) => item.id === tokenId) || null;
  }

  normalizeFoundationTokenName(value) {
    return String(value || "")
      .trim()
      .toLowerCase()
      .replace(/[\s._]+/g, "-")
      .replace(/[^a-z0-9-]/g, "")
      .replace(/-{2,}/g, "-")
      .replace(/^-+|-+$/g, "");
  }

  summarizeFoundationTokenValue(token) {
    if (!token) {
      return "";
    }

    const value = token.value;
    if (value && typeof value === "object" && !Array.isArray(value)) {
      if (["mobile", "tablet", "desktop"].some((key) => Object.prototype.hasOwnProperty.call(value, key))) {
        return `Mobile ${this.formatTokenValue(value.mobile || "")} | Tablet ${this.formatTokenValue(value.tablet || "")} | Desktop ${this.formatTokenValue(value.desktop || "")}`;
      }

      if (Object.prototype.hasOwnProperty.call(value, "value") && Object.prototype.hasOwnProperty.call(value, "unit")) {
        return this.formatTokenValue(value);
      }
    }

    return token.resolvedValue || token.rawValue || this.formatTokenValue(value);
  }

  createFoundationTokenDraft(token) {
    return token ? JSON.parse(JSON.stringify(token)) : null;
  }

  createFoundationSidebarToken(action) {
    const paletteOption = this.foundation?.paletteOptions?.[0] || { alias: "blue-500", label: "Blue 500", value: "#0000ff" };

    if (action === "palette-color") {
      return {
        id: `color-${Date.now()}`,
        section: "colours",
        propertyAlias: "color",
        label: paletteOption.label,
        path: `color.${paletteOption.alias}`,
        type: "color",
        editorKind: "paletteColor",
        value: paletteOption.value,
        paletteAlias: paletteOption.alias,
        rawValue: paletteOption.value,
        resolvedValue: paletteOption.value,
        cssVariable: `--color-${paletteOption.alias}`,
        removable: false,
      };
    }

    if (action === "custom-color") {
      return {
        id: `color-${Date.now()}`,
        section: "colours",
        propertyAlias: "color",
        label: "New Colour",
        name: "new-colour",
        path: "color.new-colour",
        type: "color",
        editorKind: "customColor",
        value: "#000000",
        rawValue: "#000000",
        resolvedValue: "#000000",
        cssVariable: "--color-new-colour",
        removable: true,
      };
    }

    if (action === "spacing") {
      return {
        id: `spacing-${Date.now()}`,
        section: "spacing",
        propertyAlias: "space",
        label: "New Spacing",
        name: "new-spacing",
        path: "space.new-spacing",
        type: "dimension",
        editorKind: "responsive",
        value: { mobile: "8px", tablet: "12px", desktop: "16px" },
        rawValue: "8px | 12px | 16px",
        resolvedValue: "8px | 12px | 16px",
        cssVariable: "--space-new-spacing",
        removable: true,
      };
    }

    return null;
  }

  syncFoundationDraftToken(token) {
    if (!token) {
      return token;
    }

    if (token.editorKind === "paletteColor") {
      const option = (this.foundation?.paletteOptions || []).find((item) => item.alias === token.paletteAlias) || null;
      token.label = option?.label || token.label || token.paletteAlias || "";
      token.path = `color.${token.paletteAlias || ""}`;
      token.cssVariable = `--color-${token.paletteAlias || ""}`;
      token.value = option?.value || token.value || "";
      token.rawValue = String(token.value || "");
      token.resolvedValue = token.rawValue;
      return token;
    }

    if (token.editorKind === "customColor") {
      const normalized = this.normalizeFoundationTokenName(token.name || token.label || "custom-colour");
      token.name = normalized;
      token.label = token.label || normalized;
      token.path = `color.${normalized}`;
      token.cssVariable = `--color-${normalized}`;
      token.rawValue = String(token.value || "");
      token.resolvedValue = token.rawValue;
      return token;
    }

    if (token.section === "spacing" && token.removable) {
      const normalized = this.normalizeFoundationTokenName(token.name || token.label || "spacing");
      token.name = normalized;
      token.label = token.label || normalized;
      token.path = `space.${normalized}`;
      token.cssVariable = `--space-${normalized}`;
    }

    token.rawValue = this.summarizeFoundationTokenValue(token);
    token.resolvedValue = token.rawValue;
    return token;
  }

  syncFoundationSidebarDraftToken() {
    const token = this.foundationSidebar?.draftToken;
    if (!token) {
      return;
    }
    this.syncFoundationDraftToken(token);
  }


  openFoundationSidebar(mode, sectionId, tokenId = "") {
    const token = tokenId ? this.findFoundationToken(sectionId, tokenId) : this.createFoundationSidebarToken(mode);
    if (!token) {
      return;
    }

    this.foundationSidebar = {
      open: true,
      mode,
      sectionId,
      tokenId,
      draftToken: this.createFoundationTokenDraft(token),
    };
    this.syncFoundationSidebarDraftToken();
    this.render();
  }

  closeFoundationSidebar() {
    this.foundationSidebar = {
      open: false,
      mode: "",
      sectionId: "",
      tokenId: "",
      draftToken: null,
    };
    this.render();
  }

  updateFoundationSidebarDraft(mutator) {
    if (!this.foundationSidebar?.draftToken) {
      return;
    }

    mutator(this.foundationSidebar.draftToken);
    this.syncFoundationSidebarDraftToken();
    this.render();
  }

  saveFoundationSidebar() {
    const draftToken = this.createFoundationTokenDraft(this.foundationSidebar?.draftToken);
    const sectionId = this.foundationSidebar?.sectionId;
    if (!draftToken || !sectionId) {
      return;
    }

    const isNewToken = !this.foundationSidebar.tokenId;
    this.foundationSidebar = {
      open: false,
      mode: "",
      sectionId: "",
      tokenId: "",
      draftToken: null,
    };
    this.updateFoundation((next) => {
      const items = next.sections?.[sectionId] || [];
      const index = items.findIndex((item) => item.id === draftToken.id);
      if (index >= 0) {
        items[index] = draftToken;
      } else {
        items.push(draftToken);
      }
      next.sections[sectionId] = items;
    }, "");
  }

  deleteFoundationSidebarToken() {
    const tokenId = this.foundationSidebar?.draftToken?.id;
    const sectionId = this.foundationSidebar?.sectionId;
    if (!tokenId || !sectionId) {
      return;
    }

    this.foundationSidebar = {
      open: false,
      mode: "",
      sectionId: "",
      tokenId: "",
      draftToken: null,
    };
    this.updateFoundation((next) => {
      next.sections[sectionId] = (next.sections?.[sectionId] || []).filter((item) => item.id !== tokenId);
    }, "");
  }

  getActivePickerContext() {
    return this.picker.contextMode === "auto" ? this.picker.detectedContext : this.picker.contextMode;
  }

  detectJsonFieldContext(json, cursorIndex) {
    const beforeCursor = String(json || "").slice(0, cursorIndex);
    const currentLine = beforeCursor.split(/\r?\n/).pop() || "";
    const fieldMatch = currentLine.match(/"([^"]+)"\s*:\s*[^,\]}]*$/);
    const fieldName = fieldMatch?.[1] || "";
    if (!fieldName) {
      return "";
    }

    const tokenTypeMatches = [...beforeCursor.matchAll(/"\$type"\s*:\s*"([^"]+)"/g)];
    const tokenType = tokenTypeMatches.length ? tokenTypeMatches[tokenTypeMatches.length - 1][1] : "";
    const composite = `${String(tokenType)}.${fieldName}`;

    if (PICKER_CONTEXT_OPTIONS.some((item) => item.value.toLowerCase() === composite.toLowerCase())) {
      return composite;
    }

    return {
      color: "color",
      width: "dimension",
      fontFamily: "fontFamily",
      fontWeight: "fontWeight",
      fontSize: "dimension",
      lineHeight: "number",
      letterSpacing: "dimension",
      offsetX: "dimension",
      offsetY: "dimension",
      blur: "dimension",
      spread: "dimension",
      duration: "duration",
      number: "number",
    }[fieldName] || "";
  }

  scopePreviewCss(css) {
    return String(css || "")
      .replaceAll(":root", ".preview-scope")
      .replaceAll("[data-theme=", ".preview-scope[data-theme=");
  }

  getEditorThemeVariantState() {
    const enabled = new Set(["default"]);

    try {
      const parsed = JSON.parse(this.editorJson || "{}");
      const configured = Array.isArray(parsed?.enabledThemeVariants) ? parsed.enabledThemeVariants : [];
      configured.forEach((alias) => {
        const match = ALLOWED_THEME_VARIANTS.find((item) => item.alias === alias);
        if (match) {
          enabled.add(match.alias);
        }
      });
    } catch {
      return ALLOWED_THEME_VARIANTS.map((variant) => ({
        ...variant,
        enabled: variant.isDefault,
      }));
    }

    return ALLOWED_THEME_VARIANTS.map((variant) => ({
      ...variant,
      enabled: variant.isDefault || enabled.has(variant.alias),
    }));
  }

  updateThemeVariantSelection(alias, enabled) {
    const variant = ALLOWED_THEME_VARIANTS.find((item) => item.alias === alias);
    if (!variant || variant.isDefault) {
      return;
    }

    this.updateJsonDocument((parsed) => {
      const nextEnabled = new Set(["default"]);
      const configured = Array.isArray(parsed?.enabledThemeVariants) ? parsed.enabledThemeVariants : [];
      configured.forEach((value) => {
        const match = ALLOWED_THEME_VARIANTS.find((item) => item.alias === value);
        if (match) {
          nextEnabled.add(match.alias);
        }
      });

      if (enabled) {
        nextEnabled.add(alias);
      } else {
        nextEnabled.delete(alias);
      }

      parsed.enabledThemeVariants = ALLOWED_THEME_VARIANTS
        .filter((item) => nextEnabled.has(item.alias))
        .map((item) => item.alias);
    }, `Theme variants updated: ${enabled ? `enabled ${alias}` : `disabled ${alias}`}.`);
  }

  updateJsonDocument(mutator, successMessage) {
    let parsed;

    try {
      parsed = JSON.parse(this.editorJson || "{}");
    } catch (error) {
      this.notice = this.describeError(error, "Draft JSON is invalid.");
      this.render();
      return false;
    }

    mutator(parsed);
    this.editorJson = `${JSON.stringify(parsed, null, 2)}\n`;
    this.notice = successMessage;
    this.render();
    this.queuePreview();
    return true;
  }

  ensureTokenNode(parsed, path, type) {
    const segments = String(path || "").split(".").filter(Boolean);
    let current = parsed;
    for (let index = 0; index < segments.length - 1; index += 1) {
      const segment = segments[index];
      if (!current[segment] || typeof current[segment] !== "object" || Array.isArray(current[segment])) {
        current[segment] = {};
      }
      current = current[segment];
    }

    const leaf = segments[segments.length - 1];
    if (!current[leaf] || typeof current[leaf] !== "object" || Array.isArray(current[leaf])) {
      current[leaf] = { $type: type, $value: null };
    }

    if (!current[leaf].$type) {
      current[leaf].$type = type;
    }

    return current[leaf];
  }

  commitTokenValue(path, type, value, message) {
    this.updateJsonDocument((parsed) => {
      const node = this.ensureTokenNode(parsed, path, type);
      node.$value = value;
    }, message || `Updated ${path}.`);
  }

  commitTokenSubValue(path, type, subPath, value, message) {
    this.updateJsonDocument((parsed) => {
      const node = this.ensureTokenNode(parsed, path, type);
      if (!node.$value || typeof node.$value !== "object" || Array.isArray(node.$value)) {
        node.$value = {};
      }

      const segments = String(subPath || "").split(".").filter(Boolean);
      let current = node.$value;
      for (let index = 0; index < segments.length - 1; index += 1) {
        const segment = segments[index];
        if (!current[segment] || typeof current[segment] !== "object" || Array.isArray(current[segment])) {
          current[segment] = {};
        }
        current = current[segment];
      }

      current[segments[segments.length - 1]] = value;
    }, message || `Updated ${path}.`);
  }

  tryParseEditorJson() {
    try {
      return JSON.parse(this.editorJson || "{}");
    } catch {
      return null;
    }
  }

  collectDraftTokens(parsed) {
    const items = [];

    const visit = (node, segments = []) => {
      if (!node || typeof node !== "object" || Array.isArray(node)) {
        return;
      }

      if (typeof node.$type === "string" && Object.prototype.hasOwnProperty.call(node, "$value")) {
        items.push({
          path: segments.join("."),
          type: node.$type,
          value: node.$value,
          node,
        });
        return;
      }

      Object.keys(node).forEach((key) => {
        if (key.startsWith("$") || key === "enabledThemeVariants") {
          return;
        }

        visit(node[key], [...segments, key]);
      });
    };

    visit(parsed);
    return items;
  }

  createTokenMetaMap() {
    return new Map((Array.isArray(this.tokens) ? this.tokens : []).map((token) => [token.path, token]));
  }

  getWorkbenchTokens(parsed, tokenMetaMap, section) {
    const draftTokens = this.collectDraftTokens(parsed)
      .filter((token) => !token.path.startsWith("themes.") && !token.path.startsWith("semantic.") && !token.path.startsWith("component."))
      .filter((token) => section.prefixes.some((prefix) => token.path.startsWith(prefix)))
      .sort((left, right) => left.path.localeCompare(right.path));

    return draftTokens.map((token) => ({
      ...token,
      meta: tokenMetaMap.get(token.path) || null,
    }));
  }

  getFoundationSectionId(path) {
    const normalizedPath = String(path || "");
    if (normalizedPath.startsWith("color.")) {
      return "colours";
    }

    if (normalizedPath.startsWith("font.") || normalizedPath.startsWith("typography.")) {
      return "typography";
    }

    if (normalizedPath.startsWith("space.") || normalizedPath.startsWith("spacing.")) {
      return "spacing";
    }

    if (normalizedPath.startsWith("radius.")) {
      return "radius";
    }

    if (normalizedPath.startsWith("shadow.")) {
      return "shadows";
    }

    return "";
  }

  hydrateFoundationTokens(payload, parsed, tokenMetaMap) {
    const sections = Object.fromEntries(BASE_THEME_SECTIONS.map((section) => [section.id, []]));
    const registryTokens = Array.isArray(payload?.tokens) ? payload.tokens : [];
    const registryByPath = new Map(registryTokens.map((token) => [token.path, token]));
    const draftTokens = parsed
      ? this.collectDraftTokens(parsed)
          .filter((token) => !token.path.startsWith("themes.") && !token.path.startsWith("semantic.") && !token.path.startsWith("component."))
      : [];

    draftTokens.forEach((token) => {
      const sectionId = this.getFoundationSectionId(token.path);
      if (!sectionId) {
        return;
      }

      const registry = registryByPath.get(token.path) || null;
      sections[sectionId].push({
        ...token,
        meta: tokenMetaMap.get(token.path) || null,
        registry,
        cssVariable: registry?.generatedCssVariable || this.deriveCssVariable(token.path, tokenMetaMap.get(token.path) || null),
        rawValue: registry?.rawValue || this.formatTokenValue(token.value),
        resolvedValue: registry?.resolvedValue || this.getResolvedDraftValue(parsed, token.path),
        valuePreview: registry?.valuePreview || this.formatTokenValue(token.value),
      });
    });

    Object.values(sections).forEach((tokens) => tokens.sort((left, right) => left.path.localeCompare(right.path)));

    const mappedPaths = new Set(Object.values(sections).flat().map((token) => token.path));
    const unmappedTokens = registryTokens
      .filter((token) => this.getFoundationSectionId(token.path))
      .filter((token) => !mappedPaths.has(token.path))
      .map((token) => token.path)
      .sort((left, right) => left.localeCompare(right));

    return {
      sections,
      summary: {
        totalTokensReceived: registryTokens.length,
        coloursMapped: sections.colours.length,
        typographyMapped: sections.typography.length,
        spacingMapped: sections.spacing.length,
        radiusMapped: sections.radius.length,
        shadowsMapped: sections.shadows.length,
        unmappedTokens,
      },
    };
  }

  getSemanticRoles(parsed, tokenMetaMap) {
    return SEMANTIC_ROLE_GROUPS.map((group) => ({
      ...group,
      roles: group.roles.map((definition) => {
        const token = this.findDraftToken(parsed, definition.path);
        return {
          ...definition,
          token,
          meta: tokenMetaMap.get(definition.path) || null,
        };
      }),
    }));
  }

  findDraftToken(parsed, path) {
    const segments = String(path || "").split(".").filter(Boolean);
    let current = parsed;
    for (const segment of segments) {
      if (!current || typeof current !== "object") {
        return null;
      }

      current = current[segment];
    }

    return current && typeof current === "object" && typeof current.$type === "string" && Object.prototype.hasOwnProperty.call(current, "$value")
      ? { path, type: current.$type, value: current.$value, node: current }
      : null;
  }

  getPrimitiveReferenceOptions(parsed, expectedType) {
    return this.collectDraftTokens(parsed)
      .filter((token) => !token.path.startsWith("semantic.") && !token.path.startsWith("component.") && !token.path.startsWith("themes."))
      .filter((token) => String(token.type).toLowerCase() === String(expectedType).toLowerCase())
      .sort((left, right) => left.path.localeCompare(right.path));
  }

  getResolvedDraftValue(parsed, path, seen = new Set()) {
    if (seen.has(path)) {
      return "[circular]";
    }

    const token = this.findDraftToken(parsed, path);
    if (!token) {
      return "";
    }

    seen.add(path);
    return this.resolveDraftValue(parsed, token.value, seen);
  }

  resolveDraftValue(parsed, value, seen) {
    if (typeof value === "string") {
      const match = value.match(/^\{(.+)\}$/);
      if (match) {
        return this.getResolvedDraftValue(parsed, match[1], seen);
      }

      return value;
    }

    if (Array.isArray(value)) {
      return JSON.stringify(value);
    }

    if (value && typeof value === "object") {
      if (Object.prototype.hasOwnProperty.call(value, "value") && Object.prototype.hasOwnProperty.call(value, "unit")) {
        const rawValue = this.resolveDraftValue(parsed, value.value, seen);
        return `${rawValue}${value.unit || ""}`;
      }

      const parts = Object.entries(value).map(([key, item]) => `${key}: ${this.resolveDraftValue(parsed, item, new Set(seen))}`);
      return parts.join(" | ");
    }

    return value == null ? "" : String(value);
  }

  formatTokenValue(value) {
    if (typeof value === "string") {
      return value;
    }

    if (typeof value === "number") {
      return String(value);
    }

    if (value && typeof value === "object") {
      if (Object.prototype.hasOwnProperty.call(value, "value") && Object.prototype.hasOwnProperty.call(value, "unit")) {
        return `${value.value ?? ""}${value.unit ?? ""}`;
      }

      return JSON.stringify(value);
    }

    return "";
  }

  deriveCssVariable(path, meta) {
    return meta?.generatedCssVariable || `--${String(path || "").replaceAll(".", "-")}`;
  }

  describeFriendlyLabel(path) {
    const explicitLabels = {
      "color.brand.primary": "Brand Primary",
      "color.brand.secondary": "Brand Secondary",
      "color.surface.page": "Page Surface",
      "color.text.default": "Text Default",
      "color.text.muted": "Text Muted",
      "font.family.sans": "Body Font",
      "font.weight.regular": "Regular Weight",
      "typography.body": "Body Size",
      "typography.heading": "Heading Size",
      "space.sm": "Small",
      "space.md": "Medium",
      "space.lg": "Large",
      "radius.sm": "Small",
      "radius.md": "Medium",
      "radius.lg": "Large",
      "shadow.card": "Card Shadow",
      "shadow.panel": "Panel Shadow",
    };
    if (explicitLabels[path]) {
      return explicitLabels[path];
    }

    const parts = String(path || "").split(".").filter(Boolean);
    if (!parts.length) {
      return "Token";
    }

    const tail = parts.slice(-2).join(" ");
    return tail.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/\b\w/g, (match) => match.toUpperCase());
  }

  getCurrentDraftStatus(active, draft, parsed, errorCount) {
    const hasDraftJson = Boolean(String(this.editorJson || "").trim());
    if (!hasDraftJson || !parsed) {
      return "Theme changes are currently in draft.";
    }

    if (errorCount > 0) {
      return "Theme changes are currently in draft.";
    }

    if (draft?.id || this.lastSavedDocumentId) {
      return "Starter theme loaded. Preview changes before publishing.";
    }

    if (!active) {
      return "Starter theme loaded. Preview changes before publishing.";
    }

    return "Theme changes are currently in draft.";
  }

  getDraftSummary(parsed, tokenMetaMap) {
    if (this.foundation?.summary) {
      return [
        { label: "Colours", value: this.foundation.summary.colours || 0 },
        { label: "Typography", value: this.foundation.summary.typography || 0 },
        { label: "Spacing", value: this.foundation.summary.spacing || 0 },
        { label: "Semantic Roles", value: this.collectDraftTokens(parsed).filter((token) => token.path.startsWith("semantic.")).length },
      ];
    }

    const colours = this.getWorkbenchTokens(parsed, tokenMetaMap, BASE_THEME_SECTIONS[0]).length;
    const typography = this.getWorkbenchTokens(parsed, tokenMetaMap, BASE_THEME_SECTIONS[1]).length;
    const spacing = this.getWorkbenchTokens(parsed, tokenMetaMap, BASE_THEME_SECTIONS[2]).length;
    const semanticRoles = this.collectDraftTokens(parsed).filter((token) => token.path.startsWith("semantic.")).length;

    return [
      { label: "Colours", value: colours },
      { label: "Typography", value: typography },
      { label: "Spacing", value: spacing },
      { label: "Semantic Roles", value: semanticRoles },
    ];
  }

  computeOverview(active, draft, latestBuild, themeVariants, parsed, tokenMetaMap) {
    const errors = this.countDiagnostics((this.preview.response || this.validation)?.errors || []);
    const hasDraftChanges = String(this.editorJson || "") !== String(active?.json || "");
    const activeVariants = themeVariants.filter((variant) => variant.enabled).map((variant) => variant.label);
    const summary = parsed ? this.getDraftSummary(parsed, tokenMetaMap) : [];
    const totalValues = summary.reduce((count, item) => count + item.value, 0);

    let nextAction = "Preview your changes, then publish when ready.";
    if (errors > 0) {
      nextAction = "Fix blocking issues, then preview the draft again.";
    } else if (hasDraftChanges && !this.preview.updatedFromDraft) {
      nextAction = "Preview changes before publishing.";
    } else if (hasDraftChanges) {
      nextAction = "Save the draft when you want to keep these changes.";
    } else if (!active && draft) {
      nextAction = "Preview changes to review the starter theme.";
    } else if (!active) {
      nextAction = "Preview changes to review the starter theme.";
    } else if (latestBuild?.success) {
      nextAction = "Adjust values, preview the draft, then publish when ready.";
    }

    let draftMessage = "Preview changes safely before publishing to the live site.";
    if (errors > 0) {
      draftMessage = "Fix draft issues, then preview again.";
    } else if (hasDraftChanges && !this.preview.updatedFromDraft) {
      draftMessage = "Theme changes are currently in draft.";
    } else if (draft?.status === "Invalid") {
      draftMessage = "Review the imported JSON and resolve invalid values.";
    } else if (this.preview.updatedFromDraft) {
      draftMessage = "Preview changes safely before publishing to the live site.";
    }

    let currentDraftLabel = "Draft saved";
    if (draft?.status === "Invalid") {
      currentDraftLabel = "Draft needs fixes";
    } else if (!draft && !active) {
      currentDraftLabel = "Starter draft";
    } else if (hasDraftChanges) {
      currentDraftLabel = "Draft has unsaved changes";
    } else if (draft || this.lastSavedDocumentId) {
      currentDraftLabel = "Draft saved";
    } else if (errors > 0) {
      currentDraftLabel = "Draft needs fixes";
    } else {
      currentDraftLabel = "Ready to edit";
    }

    return {
      currentDraftStatus: this.getCurrentDraftStatus(active, draft, parsed, errors),
      currentDraftLabel,
      currentDraftMessage: draftMessage,
      totalValues,
      summary,
      activeThemeStatus: active?.status || "No active theme",
      lastSuccessfulBuild: this.formatDate(active?.latestSuccessfulBuildDateUtc || this.status?.latestSuccessfulBuildDateUtc),
      generatedCssPath: active?.generatedCssPath || latestBuild?.generatedCssPath || "",
      enabledVariants: activeVariants.length ? activeVariants.join(", ") : "Default only",
      nextAction,
    };
  }

  render() {
    if (!this.shadowRoot) {
      return;
    }

    const active = this.active || this.status?.activeDocument || null;
    const draft = this.draft || this.status?.draftDocument || null;
    const latestBuild = this.status?.latestBuild || this.validation?.buildReport || null;
    const currentFeedback = this.preview.response || this.validation;
    const parsed = this.tryParseEditorJson();
    const errors = this.groupDiagnostics(currentFeedback?.errors || []);
    const warnings = Array.isArray(currentFeedback?.warnings) ? currentFeedback.warnings : [];
    const infos = Array.isArray(currentFeedback?.infos) ? currentFeedback.infos : [];
    const themeVariants = this.getEditorThemeVariantState();
    const enabledPreviewThemes = (this.preview.response?.themeVariants || []).filter((variant) => variant.enabled);
    const filteredTokens = this.getFilteredTokens(this.tokens);
    const tokenMetaMap = this.createTokenMetaMap();
    const overview = this.computeOverview(active, draft, latestBuild, themeVariants, parsed, tokenMetaMap);
    const foundationHydration = {
      sections: this.foundation?.sections || this.createEmptyFoundationSections(),
      summary: this.foundation?.summary || this.summarizeFoundationSections(this.foundation?.sections || this.createEmptyFoundationSections()),
    };
    const generatedCss = currentFeedback?.generatedCss || this.preview.response?.previewCss || "";
    const tailwindJson = currentFeedback?.tailwindJson || "";

    this.shadowRoot.innerHTML = `
      <style>
        :host { display:block; color:var(--uui-color-text, #1b1b1f); font-family:var(--uui-font-family, Arial, sans-serif); }
        * { box-sizing:border-box; }
        .shell { display:grid; gap:16px; padding:16px; background:var(--uui-color-surface, #f3f3f5); }
        .hero { display:grid; gap:6px; padding:0; background:none; border:0; box-shadow:none; color:inherit; }
        .hero h1, .panel h2, .panel h3, .panel h4 { margin:0; font-weight:600; color:#1b1b1f; }
        .hero p { margin:0; max-width:72ch; color:#5c5e66; font-size:.95rem; line-height:1.5; }
        .hero-copy { display:grid; gap:4px; }
        .hero-status-line { justify-self:start; padding:0; background:none; border:0; color:#5c5e66; font-size:.9rem; }
        .tab-bar, .toolbar, .variant-toolbar, .theme-toolbar { display:flex; flex-wrap:wrap; gap:8px; }
        .grid { display:grid; grid-template-columns:minmax(0, 1.15fr) minmax(320px, .85fr); gap:18px; align-items:start; }
        .editor-layout { display:grid; grid-template-columns:minmax(0, 1.2fr) minmax(320px, .8fr); gap:18px; align-items:start; }
        .stack { display:grid; gap:16px; }
        .panel { padding:16px; border-radius:6px; background:#fff; border:1px solid #d8d8dc; box-shadow:none; }
        .panel-head { display:flex; justify-content:space-between; gap:12px; align-items:flex-start; margin-bottom:12px; }
        .eyebrow { margin:0 0 4px; color:#6b6f76; font-size:.75rem; font-weight:600; letter-spacing:.02em; text-transform:none; }
        .muted, .small-note { color:#5c5e66; }
        .small-note { font-size:.9rem; line-height:1.5; }
        .notice { padding:10px 12px; border-radius:4px; background:#f6f6f7; color:#3d3f45; border:1px solid #d8d8dc; }
        .pill { display:inline-flex; align-items:center; padding:4px 8px; border-radius:999px; background:#f3f3f5; color:#3d3f45; font-weight:600; font-size:.8rem; border:1px solid #d8d8dc; }
        .empty { color:#778296; font-style:italic; }
        .meta-grid { display:grid; grid-template-columns:repeat(2, minmax(0, 1fr)); gap:10px; }
        .meta-card { padding:10px 12px; border-radius:4px; background:#fff; border:1px solid #e5e5e8; }
        .meta-card strong { display:block; margin-bottom:4px; color:#5c5e66; font-size:.76rem; letter-spacing:.02em; text-transform:none; }
        .summary-grid { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:12px; }
        .summary-card { padding:12px; border-radius:4px; background:#fff; border:1px solid #e5e5e8; }
        .summary-card strong { display:block; margin-bottom:8px; color:#5c5e66; font-size:.8rem; text-transform:none; letter-spacing:.02em; }
        .summary-card-value { font-size:1.4rem; font-weight:700; line-height:1; }
        .summary-card-note { margin-top:6px; color:#5c6a7d; font-size:.92rem; }
        .token-section { display:grid; gap:12px; }
        .token-grid { display:grid; gap:12px; }
        .token-card, .role-card { display:grid; gap:12px; padding:14px; border-radius:18px; background:#fbfaf7; border:1px solid #ece4d8; }
        .token-card { gap:14px; }
        .status-bar { display:flex; flex-wrap:wrap; gap:10px; align-items:center; }
        .status-note { color:#5c5e66; font-size:.92rem; margin-left:auto; }
        .accordion { border:1px solid #d8d8dc; border-radius:6px; background:#fff; overflow:hidden; }
        .accordion summary { list-style:none; display:flex; justify-content:space-between; align-items:center; gap:12px; padding:16px 18px; cursor:pointer; font-weight:700; }
        .accordion summary::-webkit-details-marker { display:none; }
        .accordion-body { padding:0 18px 18px; display:grid; gap:12px; }
        .accordion-meta { color:#5c6a7d; font-weight:400; }
        .settings-panel { display:grid; gap:12px; }
        .token-head { display:flex; justify-content:space-between; gap:12px; align-items:flex-start; }
        .token-title { display:grid; gap:4px; }
        .token-title strong { font-size:1rem; }
        .token-path { color:#6f7b8d; font-family:"Cascadia Code","SFMono-Regular",Consolas,monospace; font-size:.86rem; word-break:break-word; }
        .token-meta { display:grid; gap:4px; color:#6f7b8d; font-family:"Cascadia Code","SFMono-Regular",Consolas,monospace; font-size:.82rem; }
        .token-detail-toggle { border-top:1px solid #ece4d8; padding-top:10px; }
        .token-detail-toggle summary { color:#5c6a7d; font-size:.86rem; font-weight:700; cursor:pointer; }
        .token-detail-grid { display:grid; gap:8px; margin-top:10px; }
        .token-preview-visual { display:flex; align-items:center; gap:12px; flex-wrap:wrap; }
        .token-swatch-large { width:72px; height:72px; border-radius:18px; border:1px solid rgba(28, 52, 77, .12); box-shadow:inset 0 0 0 1px rgba(255,255,255,.35); }
        .token-input-row { display:flex; gap:10px; align-items:center; flex-wrap:wrap; }
        .token-input-row input[type="text"], .token-input-row input[type="number"] { flex:1 1 180px; }
        .token-sample { padding:16px 18px; border-radius:16px; background:#ffffff; border:1px solid #e4ddd0; }
        .token-shadow-sample { width:120px; height:72px; border-radius:18px; background:#ffffff; border:1px solid rgba(28, 52, 77, .08); }
        .layer-copy { margin:0 0 12px; color:#5c5e66; font-size:.95rem; line-height:1.5; }
        .layer-divider { display:flex; align-items:center; gap:10px; color:#6b6f76; font-size:.82rem; font-weight:600; }
        .layer-divider::before { content:""; flex:1; height:1px; background:#d8d8dc; }
        .mapping-card { padding:10px 12px; border-radius:14px; background:#f4efe5; border:1px solid #e5dac8; }
        .mapping-card strong { display:block; margin-bottom:4px; color:#293646; }
        .field, .editor-grid { display:grid; gap:8px; }
        .editor-grid.compact { grid-template-columns:repeat(2, minmax(0, 1fr)); }
        .triple { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:8px; }
        label { font-weight:700; font-size:.93rem; }
        input[type="text"], input[type="number"], input[type="file"], textarea, select { width:100%; padding:10px 12px; border-radius:4px; border:1px solid #c9c9cf; background:#ffffff; font:inherit; color:#182433; }
        textarea { min-height:340px; resize:vertical; font-family:"Cascadia Code","SFMono-Regular",Consolas,monospace; line-height:1.5; }
        input[type="color"] { width:52px; height:44px; border:0; padding:0; background:transparent; }
        button { border:1px solid #c9c9cf; border-radius:4px; padding:9px 12px; cursor:pointer; font:inherit; font-weight:600; background:#fff; color:#1b1b1f; }
        button:disabled { opacity:.6; cursor:wait; }
        .primary { background:#3544b1; color:#fff; border-color:#3544b1; }
        .secondary { background:#fff; color:#1b1b1f; border-color:#c9c9cf; }
        .subtle { background:#fff; color:#1b1b1f; border-color:#c9c9cf; }
        .active-tab { background:#fff; color:#1b1b1f; border-color:#3544b1; }
        .token-preview { display:flex; flex-wrap:wrap; gap:10px; align-items:center; }
        .swatch { width:40px; height:40px; border-radius:12px; border:1px solid rgba(28, 52, 77, .12); box-shadow:inset 0 0 0 1px rgba(255,255,255,.35); }
        .size-preview { display:flex; align-items:center; gap:10px; }
        .size-bar { min-width:32px; height:14px; border-radius:999px; background:#255b67; }
        .type-preview { padding:10px 12px; border-radius:12px; background:#ffffff; border:1px solid #e4ddd0; }
        .inline-row { display:flex; gap:8px; align-items:center; flex-wrap:wrap; }
        .source-grid { display:grid; gap:10px; }
        .source-row { display:grid; grid-template-columns:1.1fr repeat(4, minmax(0, .6fr)); gap:10px; padding:12px 14px; border-radius:4px; background:#fff; border:1px solid #e5e5e8; }
        .source-name strong { display:block; }
        .preview-frame { display:grid; gap:16px; padding:12px; border-radius:6px; background:#f9f9fa; border:1px solid #d8d8dc; }
        .preview-scope { padding:0; border-radius:6px; overflow:hidden; background:var(--semantic-surface-page-background, var(--color-surface-page, #ffffff)); color:var(--semantic-text-default, var(--color-text-default, #1a2230)); box-shadow:none; border:1px solid #e5e5e8; }
        .preview-grid { display:grid; gap:28px; padding:0 28px 32px; }
        .preview-topbar { display:flex; justify-content:space-between; align-items:center; gap:16px; padding:18px 28px; background:color-mix(in srgb, var(--semantic-surface-page-background, #ffffff) 88%, #eef4f8); border-bottom:1px solid color-mix(in srgb, var(--semantic-border-default, rgba(26,34,48,.12)) 55%, transparent); }
        .preview-nav { display:flex; gap:18px; align-items:center; color:var(--semantic-text-muted, #566478); font-size:.95rem; }
        .preview-hero { display:grid; gap:18px; padding-top:8px; }
        .preview-heading { margin:0; font-size:clamp(2.35rem, 3vw, 3.4rem); line-height:1.02; color:var(--semantic-text-default, var(--color-text-default, #1a2230)); }
        .preview-body { margin:0; font-size:var(--typography-body-font-size, 1rem); line-height:1.7; color:var(--semantic-text-muted, #566478); max-width:60ch; }
        .preview-link { color:var(--semantic-link-default, var(--color-brand-primary, #255b67)); text-decoration:underline; }
        .preview-actions { display:flex; flex-wrap:wrap; gap:12px; }
        .preview-btn { padding:12px 18px; border-radius:var(--component-button-primary-radius, 999px); border:1px solid transparent; font-weight:700; }
        .preview-btn-primary { background:var(--component-button-primary-background, var(--semantic-action-primary-background, var(--color-brand-primary, #255b67))); color:var(--component-button-primary-text, var(--semantic-action-primary-text, #ffffff)); border-color:var(--component-button-primary-border, var(--semantic-action-primary-background, #255b67)); box-shadow:var(--shadow-card, 0 12px 26px rgba(28,52,77,.16)); }
        .preview-btn-secondary { background:var(--component-button-secondary-background, #ffffff); color:var(--component-button-secondary-text, var(--color-brand-primary, #255b67)); border-color:var(--component-button-secondary-border, var(--semantic-border-default, rgba(28,52,77,.16))); }
        .preview-card { padding:22px; border-radius:var(--component-card-radius, var(--radius-lg, 18px)); background:var(--component-card-background, var(--semantic-surface-card-background, #ffffff)); border:1px solid color-mix(in srgb, var(--component-card-border, var(--semantic-border-default, rgba(28,52,77,.12))) 70%, transparent); box-shadow:var(--component-card-shadow, var(--shadow-card, 0 18px 36px rgba(28,52,77,.08))); }
        .preview-form { display:grid; gap:10px; }
        .preview-input { padding:12px 14px; border-radius:var(--component-input-radius, var(--radius-md, 12px)); border:1px solid var(--component-input-border, var(--semantic-input-border, rgba(28,52,77,.16))); background:var(--component-input-background, #ffffff); color:var(--component-input-text, var(--semantic-input-text, #1a2230)); }
        .preview-input-focus { outline:3px solid var(--component-input-focus-ring, var(--semantic-input-focus-ring, rgba(37,91,103,.28))); outline-offset:2px; }
        .preview-alert { padding:16px 18px; border-radius:18px; background:color-mix(in srgb, var(--semantic-action-primary-background, #255b67) 10%, white); border:1px solid color-mix(in srgb, var(--semantic-action-primary-background, #255b67) 18%, white); color:var(--semantic-text-default, #1a2230); }
        .preview-spacing { display:grid; gap:10px; }
        .preview-spacing-box { border-radius:12px; background:var(--semantic-action-primary-background, #255b67); }
        .preview-space-sm { width:calc(var(--space-sm, 8px) * 4); height:var(--space-sm, 8px); }
        .preview-space-md { width:calc(var(--space-md, 16px) * 4); height:var(--space-md, 16px); }
        .preview-space-lg { width:calc(var(--space-lg, 24px) * 4); height:var(--space-lg, 24px); }
        .radius-preview { width:64px; height:40px; background:#255b67; border:1px solid rgba(28, 52, 77, .12); }
        .shadow-preview { width:96px; height:56px; border-radius:16px; background:#ffffff; border:1px solid rgba(28, 52, 77, .08); }
        .list, .warning-list, .change-list { display:grid; gap:8px; margin:0; padding-left:18px; }
        .diag-group { border:1px solid #e2dacd; border-radius:16px; overflow:hidden; }
        .diag-head { padding:10px 12px; background:#f7f2e9; font-weight:700; }
        .diag-item { padding:10px 12px; border-top:1px solid #ece4d8; }
        .diag-item small { display:block; color:#5c6a7d; margin-bottom:4px; }
        .table-wrap { overflow:auto; }
        table { width:100%; border-collapse:collapse; font-size:.92rem; }
        th, td { padding:10px 8px; border-bottom:1px solid #ece4d8; text-align:left; vertical-align:top; }
        th { color:#6a543d; font-size:.78rem; text-transform:uppercase; letter-spacing:.08em; }
        tbody tr { cursor:pointer; }
        tbody tr:hover { background:#faf6ef; }
        tbody tr.active { background:#e9f2f7; }
        .detail-grid { display:grid; gap:10px; }
        .detail-card { padding:10px 12px; border-radius:14px; background:#fbfaf7; border:1px solid #ece4d8; }
        .empty-state { padding:14px 16px; border-radius:4px; background:#f9f9fa; border:1px solid #e5e5e8; color:#425063; }
        .empty-state strong { display:block; margin-bottom:6px; color:#293646; }
        pre { margin:0; white-space:pre-wrap; word-break:break-word; font-family:"Cascadia Code","SFMono-Regular",Consolas,monospace; font-size:.88rem; }
        details summary { cursor:pointer; font-weight:700; }
        .modal-backdrop { position:fixed; inset:0; background:rgba(26,34,48,.45); display:flex; align-items:center; justify-content:center; padding:24px; z-index:1000; }
        .modal { width:min(980px,100%); max-height:min(86vh,100%); overflow:auto; padding:20px; border-radius:6px; background:#ffffff; box-shadow:0 8px 30px rgba(0,0,0,.18); }
        .picker-results { border:1px solid #e2dacd; border-radius:16px; overflow:hidden; }
        .picker-item { display:grid; grid-template-columns:minmax(0,1.4fr) 120px minmax(0,1fr) auto; gap:12px; align-items:center; padding:12px 14px; border-top:1px solid #ece4d8; }
        .picker-item:first-child { border-top:0; }
        .picker-item:hover { background:#faf6ef; }
        .picker-path { font-weight:700; word-break:break-word; }
        .picker-preview { color:#5c6a7d; word-break:break-word; }
        .picker-controls { display:grid; grid-template-columns:minmax(0,1fr) minmax(220px,260px) minmax(220px,260px); gap:10px; }
        .foundation-section-toolbar { display:flex; gap:8px; flex-wrap:wrap; margin-bottom:12px; }
        .foundation-list { border:1px solid #d9d9db; border-radius:6px; overflow:hidden; background:#fff; }
        .foundation-row { display:grid; grid-template-columns:40px minmax(0,1.3fr) minmax(0,1fr) auto auto; gap:12px; align-items:center; padding:10px 12px; border-top:1px solid #e5e5e8; }
        .foundation-row:first-child { border-top:0; }
        .foundation-row:hover { background:#f5f5f7; }
        .foundation-row-preview { display:flex; align-items:center; justify-content:center; min-height:24px; }
        .foundation-row-label strong, .foundation-row-value strong { display:block; }
        .foundation-row-label div, .foundation-row-value div, .foundation-row-path { color:#6b6f76; font-size:12px; word-break:break-word; }
        .foundation-row-actions { display:flex; justify-content:flex-end; gap:8px; align-items:center; }
        .foundation-badge { display:inline-flex; align-items:center; border:1px solid #d9d9db; border-radius:999px; padding:2px 8px; font-size:11px; color:#6b6f76; background:#fff; }
        .foundation-swatch { width:20px; height:20px; border-radius:3px; border:1px solid rgba(26,34,48,.16); box-shadow:inset 0 0 0 1px rgba(255,255,255,.2); }
        .foundation-mini-bar { height:8px; border-radius:999px; background:#6b8cff; }
        .foundation-preview-chip { width:32px; height:20px; border-radius:8px; border:1px solid rgba(26,34,48,.12); background:#f6f6f7; }
        .foundation-shadow-chip { width:32px; height:20px; border-radius:6px; background:#fff; }
        .foundation-sidebar-shell { width:min(640px,100%); height:100%; margin-left:auto; display:flex; justify-content:flex-end; }
        .foundation-sidebar { display:block; width:100%; max-width:640px; height:100%; background:#fff; box-shadow:-12px 0 30px rgba(0,0,0,.14); overflow:hidden; }
        .foundation-sidebar-layout { display:grid; grid-template-rows:auto minmax(0,1fr) auto; height:100%; }
        .foundation-sidebar-header { padding:16px 20px 12px; border-bottom:1px solid #e5e5e8; }
        .foundation-sidebar-body { padding:16px 20px; overflow:auto; display:grid; gap:16px; align-content:start; }
        .foundation-sidebar-footer { padding:12px 20px; border-top:1px solid #e5e5e8; background:#fff; position:sticky; bottom:0; display:flex; justify-content:space-between; align-items:center; gap:12px; }
        .foundation-sidebar-footer-actions { display:flex; gap:8px; margin-left:auto; }
        .foundation-sidebar-preview { padding:0; border:0; background:none; }
        .foundation-sidebar-preview-row { display:flex; align-items:center; gap:10px; }
        .foundation-sidebar-preview-meta { font-size:12px; color:#6b6f76; }
        .foundation-details summary { cursor:pointer; color:#1b4b91; }
        .foundation-field-grid { display:grid; gap:12px; }
        .foundation-palette-grid { display:grid; gap:10px; }
        .foundation-palette-row { display:grid; grid-template-columns:84px minmax(0,1fr); gap:10px; align-items:start; }
        .foundation-palette-family { font-size:12px; color:#6b6f76; padding-top:6px; }
        .foundation-palette-swatches { display:grid; grid-template-columns:repeat(auto-fill,minmax(32px,1fr)); gap:6px; }
        .foundation-palette-button { display:grid; gap:4px; justify-items:center; background:none; border:0; padding:0; cursor:pointer; }
        .foundation-palette-chip { width:32px; height:32px; border-radius:4px; border:1px solid rgba(26,34,48,.12); box-sizing:border-box; }
        .foundation-palette-button.selected .foundation-palette-chip { outline:2px solid #3544b1; outline-offset:1px; }
        .foundation-palette-label { font-size:10px; color:#6b6f76; line-height:1; }
        .foundation-field-help { margin-top:4px; font-size:12px; color:#6b6f76; }
        .foundation-disclosure { border-top:1px solid #e5e5e8; padding-top:12px; }
        @media (max-width: 1180px) { .grid, .editor-layout { grid-template-columns:1fr; } }
        @media (max-width: 900px) { .summary-grid, .source-row, .editor-grid.compact, .triple, .picker-controls, .picker-item, .foundation-row { grid-template-columns:1fr; } .status-bar { flex-direction:column; align-items:stretch; } .status-note { margin-left:0; } .preview-topbar { padding:16px 20px; } .preview-grid { padding:0 20px 24px; } .foundation-sidebar-shell { width:100%; } }
        @media (max-width: 720px) { .shell { padding:14px; } .hero, .panel, .modal { padding:16px; } .meta-grid { grid-template-columns:1fr; } .modal-backdrop { padding:0; } .foundation-sidebar { max-width:none; } .foundation-sidebar-header, .foundation-sidebar-body, .foundation-sidebar-footer { padding-left:16px; padding-right:16px; } }
      </style>
      <div class="shell">
        <section class="hero">
          <div class="hero-copy">
            <h1>Theme Workbench</h1>
            <p>Manage foundation tokens and semantic roles for this site's theme.</p>
          </div>
          <div class="hero-status-line">${this.escapeHtml(overview.currentDraftStatus)}</div>
        </section>
        <div class="tab-bar">
          <uui-button label="Foundation Tokens" look="${this.activeTab === "base" ? "primary" : "secondary"}" data-tab="base">Foundation Tokens</uui-button>
          <uui-button label="Semantic Roles" look="${this.activeTab === "semantic" ? "primary" : "secondary"}" data-tab="semantic">Semantic Roles</uui-button>
        </div>
        ${this.notice ? `<div class="notice">${this.escapeHtml(this.notice)}</div>` : ""}
        ${this.renderMainTabs(parsed, active, latestBuild, currentFeedback, warnings, infos, themeVariants, enabledPreviewThemes, overview, tokenMetaMap, foundationHydration, errors, filteredTokens, generatedCss, tailwindJson)}
        ${this.picker.open ? this.renderPickerModal() : ""}
        ${this.foundationSidebar?.open ? this.renderFoundationSidebar() : ""}
      </div>
    `;

    this.bindEvents();
  }

  renderWorkbench(parsed, active, latestBuild, currentFeedback, warnings, infos, themeVariants, enabledPreviewThemes, overview, tokenMetaMap) {
    return `
      <div class="stack">
        <section class="panel">
          <div class="panel-head">
            <div>
              <p class="eyebrow">Overview</p>
              <h2>Theme status</h2>
            </div>
            ${this.loading.init ? `<span class="pill">Loading</span>` : ""}
          </div>
          <div class="summary-grid">
            <div class="summary-card"><strong>Purpose</strong><div>Curated editor for primitives, semantic roles, safe preview, and CSS publishing.</div></div>
            <div class="summary-card"><strong>Active theme status</strong><div>${this.escapeHtml(overview.activeThemeStatus)}</div></div>
            <div class="summary-card"><strong>Last successful build</strong><div>${this.escapeHtml(overview.lastSuccessfulBuild || "None yet")}</div></div>
            <div class="summary-card"><strong>Generated CSS path</strong><div>${this.escapeHtml(overview.generatedCssPath || "Unknown")}</div></div>
            <div class="summary-card"><strong>Enabled variants</strong><div>${this.escapeHtml(overview.enabledVariants)}</div></div>
            <div class="summary-card"><strong>Next action</strong><div>${this.escapeHtml(overview.nextAction)}</div></div>
          </div>
        </section>
        <div class="grid">
          <div class="stack">
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Base Theme</p>
                  <h2>Primitives</h2>
                </div>
                <span class="pill">${parsed ? "Draft ready" : "JSON invalid"}</span>
              </div>
              <p class="small-note">Editors should start here. Each token shows current value, editable control, CSS variable, and a quick preview.</p>
              ${parsed
                ? BASE_THEME_SECTIONS.map((section) => this.renderBaseThemeSection(parsed, tokenMetaMap, section)).join("")
                : `<div class="notice">Draft JSON is invalid. Raw JSON can be fixed in Advanced.</div>`}
            </section>
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Semantic Roles</p>
                  <h2>Intent-based theme roles</h2>
                </div>
              </div>
              <p class="small-note">Map semantic roles to primitive references first. Direct values still allowed when needed.</p>
              ${parsed ? this.renderSemanticRoles(parsed, tokenMetaMap) : `<p class="empty">Semantic roles need valid draft JSON.</p>`}
            </section>
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Token Sources</p>
                  <h2>Winning source visibility</h2>
                </div>
              </div>
              <p class="small-note">Normal overrides are expected. Counts below show which source won, not a warning tally.</p>
              ${this.renderSourceOverview(currentFeedback)}
            </section>
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Publish Flow</p>
                  <h2>Draft to generated CSS</h2>
                </div>
              </div>
              <p class="small-note">Preview never affects the live site. Publish writes generated CSS only after validation succeeds.</p>
              <div class="variant-toolbar">
                ${themeVariants.map((variant) => `
                  <label class="meta-card">
                    <strong>${this.escapeHtml(variant.label)}</strong>
                    <div class="inline-row">
                      <input type="checkbox" data-theme-variant="${this.escapeAttr(variant.alias)}" ${variant.enabled ? "checked" : ""} ${variant.isDefault ? "disabled" : ""} />
                      <span>${this.escapeHtml(variant.selector)}</span>
                    </div>
                  </label>`).join("")}
              </div>
              <div class="toolbar" style="margin-top:14px;">
                <button class="subtle" data-action="validate" ${this.loading.validate ? "disabled" : ""}>${this.loading.validate ? "Validating..." : "Validate"}</button>
                <button class="secondary" data-action="preview" ${this.loading.preview ? "disabled" : ""}>${this.loading.preview ? "Previewing..." : "Preview"}</button>
                <button class="subtle" data-action="save" ${this.loading.save ? "disabled" : ""}>${this.loading.save ? "Saving..." : "Save Draft"}</button>
                <button class="primary" data-action="activate" ${this.loading.activate ? "disabled" : ""}>${this.loading.activate ? "Publishing..." : "Publish / Activate"}</button>
                <button class="secondary" data-action="rebuild" ${this.loading.rebuild ? "disabled" : ""}>${this.loading.rebuild ? "Rebuilding..." : "Rebuild"}</button>
              </div>
              ${currentFeedback?.buildReport ? `
                <div class="meta-grid" style="margin-top:14px;">
                  ${this.renderMeta("Success", String(currentFeedback.buildReport.success))}
                  ${this.renderMeta("Errors", String(currentFeedback.buildReport.errorCount || 0))}
                  ${this.renderMeta("Warnings", String(currentFeedback.buildReport.warningCount || 0))}
                  ${this.renderMeta("Info", String(currentFeedback.buildReport.infoCount || 0))}
                </div>` : ""}
            </section>
          </div>
          <div class="stack">
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Preview Pane</p>
                  <h2>Live draft preview</h2>
                </div>
                <span class="pill">${this.preview.updatedFromDraft ? "Draft preview" : "Preview idle"}</span>
              </div>
              <div class="theme-toolbar">
                ${(enabledPreviewThemes.length ? enabledPreviewThemes : [{ alias: "default", name: "Default" }]).map((variant) => `
                  <button class="${this.preview.theme === variant.alias ? "secondary" : "subtle"}" data-preview-theme="${this.escapeAttr(variant.alias)}">${this.escapeHtml(variant.name || variant.alias)}</button>`).join("")}
              </div>
              <div class="preview-frame">
                ${this.preview.response?.comparison ? this.renderComparison(this.preview.response.comparison) : `<p class="small-note">Run Preview or edit curated fields to refresh the safe draft preview.</p>`}
                <div class="preview-scope" ${this.preview.theme && this.preview.theme !== "default" ? `data-theme="${this.escapeAttr(this.preview.theme)}"` : ""}>
                  <style>${this.preview.scopedCss}</style>
                  ${this.renderPreviewContent()}
                </div>
              </div>
            </section>
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Active Theme</p>
                  <h2>Published output</h2>
                </div>
              </div>
              ${active ? `
                <div class="meta-grid">
                  ${this.renderMeta("Document", active.name)}
                  ${this.renderMeta("Status", active.status)}
                  ${this.renderMeta("Updated", this.formatDate(active.updatedDateUtc))}
                  ${this.renderMeta("Updated by", active.updatedBy || this.status?.latestBuildUpdatedBy || "")}
                  ${this.renderMeta("Token count", String(active.tokenCount || 0))}
                  ${this.renderMeta("CSS path", active.generatedCssPath || "")}
                  ${this.renderMeta("Last successful build", this.formatDate(active.latestSuccessfulBuildDateUtc))}
                  ${this.renderMeta("Tailwind path", active.generatedTailwindPath || "")}
                </div>` : `<p class="empty">No active token document.</p>`}
            </section>
            <section class="panel">
              <div class="panel-head">
                <div>
                  <p class="eyebrow">Editor Notes</p>
                  <h2>Safe guidance</h2>
                </div>
              </div>
              <ul class="warning-list">
                <li>Curated primitives and semantic roles come first. Raw JSON stays in Advanced.</li>
                <li>Preview builds isolated CSS only. No live CSS write.</li>
                <li>Publish updates generated CSS after the token pipeline validates successfully.</li>
                <li>Diagnostics and merge traces remain available for developers in Advanced.</li>
              </ul>
            </section>
          </div>
        </div>
      </div>
    `;
  }

  renderMainTabs(parsed, active, latestBuild, currentFeedback, warnings, infos, themeVariants, enabledPreviewThemes, overview, tokenMetaMap, foundationHydration, errors, filteredTokens, generatedCss, tailwindJson) {
    const developerDetails = this.renderDeveloperDetails(active, currentFeedback, foundationHydration.summary, errors, warnings, infos, filteredTokens, generatedCss, tailwindJson);
    return `
      <div class="editor-layout">
        <div class="stack">
          ${this.renderPublishStrip(currentFeedback, overview)}
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">${this.activeTab === "semantic" ? "Semantic Roles" : "Foundation Tokens"}</p>
                <h2>${this.activeTab === "semantic" ? "Semantic Roles" : "Foundation Tokens"}</h2>
              </div>
              <span class="pill">${parsed ? "Draft ready" : "JSON invalid"}</span>
            </div>
            <p class="layer-copy">${this.activeTab === "semantic"
              ? "Map foundation tokens to UI meaning, such as page background, text colour and button styling."
              : "Define the reusable base values for this theme. Semantic roles and components should build from these values."}</p>
            ${this.activeTab === "semantic"
              ? (parsed
                ? this.renderSemanticRoles(parsed, tokenMetaMap)
                : `<div class="notice">Draft JSON is invalid. Fix raw JSON in Developer Details.</div>`)
              : BASE_THEME_SECTIONS.map((section) => this.renderBaseThemeSection(foundationHydration, section)).join("")}
          </section>
          <div class="layer-divider">${this.activeTab === "semantic" ? "Semantic Roles -> Live Preview" : "Foundation Tokens -> Semantic Roles"}</div>
          ${this.renderThemeSettings(themeVariants)}
          ${developerDetails}
        </div>
        <div class="stack">
          ${this.renderPreviewPanel(enabledPreviewThemes)}
        </div>
      </div>
    `;
  }

  renderPublishStrip(currentFeedback, overview) {
    const canPublish = Boolean(
      this.preview.updatedFromDraft &&
      this.preview.response?.success &&
      currentFeedback?.buildReport?.success
    );
    const previewStatus = this.loading.preview
      ? "Updating preview"
      : this.preview.updatedFromDraft
        ? "Preview ready"
        : "Not previewed yet";

    return `
      <section class="panel">
        <div class="status-bar">
          <uui-button label="${this.loading.preview ? "Previewing" : "Preview Changes"}" look="secondary" data-action="preview-theme" ${this.loading.preview ? "disabled" : ""}>${this.loading.preview ? "Previewing..." : "Preview Changes"}</uui-button>
          <uui-button label="${this.loading.activate ? "Publishing" : "Publish Theme"}" look="primary" data-action="activate" ${this.loading.activate || !canPublish ? "disabled" : ""}>${this.loading.activate ? "Publishing..." : "Publish Theme"}</uui-button>
          <div class="status-note">${this.escapeHtml(this.loading.preview ? previewStatus : canPublish ? overview.currentDraftStatus : overview.currentDraftMessage)}</div>
        </div>
      </section>
    `;
  }

  renderThemeSettings(themeVariants) {
    return `
      <section class="panel">
        <details class="settings-panel">
          <summary>Theme Settings</summary>
          <div class="variant-toolbar">
            ${themeVariants.map((variant) => `
              <label class="meta-card">
                <strong>${this.escapeHtml(variant.label)}</strong>
                <div class="inline-row">
                  <input type="checkbox" data-theme-variant="${this.escapeAttr(variant.alias)}" ${variant.enabled ? "checked" : ""} ${variant.isDefault ? "disabled" : ""} />
                  <span>${this.escapeHtml(variant.selector)}</span>
                </div>
              </label>`).join("")}
          </div>
        </details>
      </section>
    `;
  }

  renderPreviewPanel(enabledPreviewThemes) {
    return `
      <section class="panel">
          <div class="panel-head">
          <div>
            <p class="eyebrow">Preview</p>
            <h2>Live Preview</h2>
          </div>
          <span class="pill">${this.preview.updatedFromDraft ? "Preview ready" : "Preview idle"}</span>
        </div>
        <div class="theme-toolbar">
          ${(enabledPreviewThemes.length ? enabledPreviewThemes : [{ alias: "default", name: "Default" }]).map((variant) => `
            <uui-button label="${this.escapeAttr(variant.name || variant.alias)}" look="${this.preview.theme === variant.alias ? "primary" : "secondary"}" data-preview-theme="${this.escapeAttr(variant.alias)}">${this.escapeHtml(variant.name || variant.alias)}</uui-button>`).join("")}
        </div>
        <div class="preview-frame">
          ${this.preview.response?.comparison ? this.renderComparison(this.preview.response.comparison) : `<p class="small-note">Preview changes to refresh this view.</p>`}
          <div class="preview-scope" ${this.preview.theme && this.preview.theme !== "default" ? `data-theme="${this.escapeAttr(this.preview.theme)}"` : ""}>
            <style>${this.preview.scopedCss}</style>
            ${this.renderPreviewContent()}
          </div>
        </div>
      </section>
    `;
  }

  renderDeveloperDetails(active, currentFeedback, hydrationSummary, errors, warnings, infos, filteredTokens, generatedCss, tailwindJson) {
    return `
      <section class="panel">
        <details>
          <summary>Developer Details</summary>
          <div class="stack" style="margin-top:14px;">
            <section class="detail-card">
              <strong>Raw JSON</strong>
              <div class="field" style="margin-top:10px;">
                <label for="doc-name">Draft name</label>
                <input id="doc-name" type="text" value="${this.escapeAttr(this.editorName)}" placeholder="Spring campaign theme" />
              </div>
              <div class="field">
                <label for="doc-file">Import token JSON</label>
                <input id="doc-file" type="file" accept=".json,application/json" />
              </div>
              <div class="field">
                <label for="doc-json">Raw token JSON</label>
                <textarea id="doc-json" spellcheck="false">${this.escapeHtml(this.editorJson)}</textarea>
                <div class="toolbar">
                  <select id="picker-context-mode">
                    ${PICKER_CONTEXT_OPTIONS.map((item) => `<option value="${this.escapeAttr(item.value)}" ${this.picker.contextMode === item.value ? "selected" : ""}>${this.escapeHtml(item.label)}</option>`).join("")}
                  </select>
                  <button class="subtle" data-action="open-picker">Insert token reference</button>
                  <button class="subtle" data-action="save" ${this.loading.save ? "disabled" : ""}>${this.loading.save ? "Saving..." : "Save Draft"}</button>
                  <button class="subtle" data-action="validate" ${this.loading.validate ? "disabled" : ""}>${this.loading.validate ? "Validating..." : "Validate"}</button>
                  <button class="subtle" data-action="rebuild" ${this.loading.rebuild ? "disabled" : ""}>${this.loading.rebuild ? "Rebuilding..." : "Rebuild"}</button>
                  <button class="subtle" data-action="export" ${this.loading.export ? "disabled" : ""}>${this.loading.export ? "Exporting..." : "Export Active JSON"}</button>
                </div>
              </div>
            </section>
            <section class="detail-card">
              <strong>Hydration Summary</strong>
              <div class="meta-grid" style="margin-top:10px;">
                ${this.renderMeta("Total tokens received", String(hydrationSummary?.totalTokensReceived || 0))}
                ${this.renderMeta("Colours mapped", String(hydrationSummary?.coloursMapped || 0))}
                ${this.renderMeta("Typography mapped", String(hydrationSummary?.typographyMapped || 0))}
                ${this.renderMeta("Spacing mapped", String(hydrationSummary?.spacingMapped || 0))}
                ${this.renderMeta("Radius mapped", String(hydrationSummary?.radiusMapped || 0))}
                ${this.renderMeta("Shadows mapped", String(hydrationSummary?.shadowsMapped || 0))}
              </div>
              ${Array.isArray(hydrationSummary?.unmappedTokens) && hydrationSummary.unmappedTokens.length
                ? `<div style="margin-top:10px;">${this.renderListMeta("Unmapped tokens", hydrationSummary.unmappedTokens)}</div>`
                : `<p class="empty" style="margin-top:10px;">No unmapped foundation tokens.</p>`}
            </section>
            <section class="detail-card">
              <strong>Diagnostics</strong>
              <div class="stack" style="margin-top:10px;">
                <div>
                  <strong>Errors</strong>
                  ${Object.keys(errors).length ? Object.entries(errors).map(([stage, items]) => this.renderErrorGroup(stage, items)).join("") : `<p class="empty">No current errors.</p>`}
                </div>
                <div>
                  <strong>Warnings</strong>
                  ${warnings.length ? `<ul class="warning-list">${warnings.map((item) => `<li><strong>${this.escapeHtml(item.tokenPath || "General")}</strong>: ${this.escapeHtml(item.message)}</li>`).join("")}</ul>` : `<p class="empty">No current warnings.</p>`}
                </div>
                <div>
                  <strong>Info</strong>
                  ${infos.length ? `<ul class="warning-list">${infos.map((item) => `<li><strong>${this.escapeHtml(item.tokenPath || "General")}</strong>: ${this.escapeHtml(item.message)}</li>`).join("")}</ul>` : `<p class="empty">No current info items.</p>`}
                </div>
              </div>
            </section>
            <section class="detail-card">
              <strong>Generated CSS Snapshot</strong>
              ${generatedCss ? `<pre style="margin-top:10px;">${this.escapeHtml(generatedCss)}</pre>` : `<p class="empty">No generated CSS snapshot.</p>`}
            </section>
            <section class="detail-card">
              <strong>Tailwind Output</strong>
              ${tailwindJson ? `<pre style="margin-top:10px;">${this.escapeHtml(tailwindJson)}</pre>` : `<p class="empty">No Tailwind output snapshot.</p>`}
            </section>
            <section class="detail-card">
              <strong>Merge Log</strong>
              ${Array.isArray(currentFeedback?.mergeEvents) && currentFeedback.mergeEvents.length ? this.renderMergeLog(currentFeedback.mergeEvents) : `<p class="empty">No merge events available.</p>`}
            </section>
            <section class="detail-card">
              <strong>Source Trace</strong>
              ${this.renderTokenFilters()}
              ${filteredTokens.length ? `
                <div class="table-wrap" style="margin-top:10px;"><table>
                  <thead>
                    <tr>
                      <th>Path</th>
                      <th>Type</th>
                      <th>Source</th>
                      <th>Priority</th>
                      <th>CSS variable</th>
                      <th>Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${filteredTokens.map((token) => `
                      <tr data-token-path="${this.escapeAttr(token.path)}" class="${this.selectedToken?.path === token.path ? "active" : ""}">
                        <td>${this.escapeHtml(token.path)}</td>
                        <td>${this.escapeHtml(token.type)}</td>
                        <td>${this.escapeHtml(token.source || token.sourceType || "")}</td>
                        <td>${this.escapeHtml(String(token.sourcePriority ?? ""))}</td>
                        <td>${this.escapeHtml(token.generatedCssVariable || "")}</td>
                        <td>${this.escapeHtml(token.valuePreview || "")}</td>
                      </tr>`).join("")}
                  </tbody>
                </table></div>` : `<p class="empty">No active build token list available.</p>`}
            </section>
            <section class="detail-card">
              <strong>Active Document</strong>
              ${active ? `
                <div class="meta-grid" style="margin-top:10px;">
                  ${this.renderMeta("Document", active.name)}
                  ${this.renderMeta("Status", active.status)}
                  ${this.renderMeta("Created", this.formatDate(active.createdDateUtc))}
                  ${this.renderMeta("Updated", this.formatDate(active.updatedDateUtc))}
                  ${this.renderMeta("Updated by", active.updatedBy || this.status?.latestBuildUpdatedBy || "")}
                  ${this.renderMeta("Token count", String(active.tokenCount || 0))}
                  ${this.renderMeta("CSS path", active.generatedCssPath || "")}
                  ${this.renderMeta("Tailwind path", active.generatedTailwindPath || "")}
                </div>` : `<p class="empty">No active token document.</p>`}
            </section>
          </div>
        </details>
      </section>
    `;
  }

  renderBaseThemeSection(foundationHydration, section) {
    const tokens = foundationHydration?.sections?.[section.id] || [];
    const emptyState = SECTION_EMPTY_STATES[section.id] || {
      title: `No ${section.label.toLowerCase()} values yet.`,
      message: `Add ${section.label.toLowerCase()} values to expand this section.`,
    };
    const isOpenByDefault = section.id === "colours";
    const addActions = section.id === "colours"
      ? `
        <div class="foundation-section-toolbar">
          <uui-button label="Add Palette Colour" look="secondary" data-foundation-add="palette-color">Add Palette Colour</uui-button>
          <uui-button label="Add Custom Colour" look="secondary" data-foundation-add="custom-color">Add Custom Colour</uui-button>
        </div>`
      : section.id === "spacing"
        ? `<div class="foundation-section-toolbar"><uui-button label="Add Spacing Token" look="secondary" data-foundation-add="spacing">Add Spacing Token</uui-button></div>`
        : "";

    return `
      <details class="accordion" ${isOpenByDefault ? "open" : ""}>
        <summary>
          <span>${this.escapeHtml(section.label)}</span>
          <span class="accordion-meta">${tokens.length ? `${tokens.length} value${tokens.length === 1 ? "" : "s"}` : ""}</span>
        </summary>
        <div class="accordion-body">
          ${addActions}
          ${tokens.length
            ? `<div class="foundation-list">${tokens.map((token) => this.renderTokenCard(token)).join("")}</div>`
            : `<div class="empty-state"><strong>${this.escapeHtml(emptyState.title)}</strong><div>${this.escapeHtml(emptyState.message)}</div></div>`}
        </div>
      </details>
    `;
  }

  renderTokenCard(token) {
    const resolvedValue = token.resolvedValue || this.formatTokenValue(token.value);
    const badge = token.section === "colours"
      ? (token.editorKind === "customColor" ? "Custom" : "Palette")
      : token.removable ? "Custom" : "Core";
    const preview = this.renderFoundationRowPreview(token, resolvedValue);
    return `
      <article class="foundation-row">
        <div class="foundation-row-preview">${preview}</div>
        <div class="foundation-row-label">
          <strong>${this.escapeHtml(token.label || this.describeFriendlyLabel(token.path))}</strong>
          <div>${this.escapeHtml(token.section === "colours" ? "Colour token" : this.describeFriendlyLabel(token.section || ""))}</div>
        </div>
        <div class="foundation-row-value">
          <strong>${this.escapeHtml(resolvedValue || "Not set")}</strong>
          <div>${this.escapeHtml(token.path || "")}</div>
        </div>
        <div><span class="foundation-badge">${this.escapeHtml(badge)}</span></div>
        <div class="foundation-row-actions">
          <uui-button look="secondary" label="Edit" data-foundation-open="${this.escapeAttr(token.id)}" data-foundation-section="${this.escapeAttr(token.section)}">Edit</uui-button>
        </div>
      </article>
    `;
  }

  getPaletteOptionGroups() {
    const groups = new Map();
    (this.foundation?.paletteOptions || []).forEach((option) => {
      const alias = String(option.alias || "");
      const match = alias.match(/^([a-z]+)-(\d{2,3})$/i);
      const family = match ? this.describeFriendlyLabel(match[1]) : option.label?.replace(/\s+\d+$/, "") || option.label || alias;
      const shade = match ? match[2] : alias.split("-").slice(1).join("-") || "";
      if (!groups.has(family)) {
        groups.set(family, []);
      }
      groups.get(family).push({
        ...option,
        family,
        shade,
      });
    });

    return Array.from(groups.entries())
      .map(([family, items]) => ({
        family,
        items: items.sort((left, right) => String(left.shade).localeCompare(String(right.shade), undefined, { numeric: true })),
      }))
      .sort((left, right) => left.family.localeCompare(right.family));
  }

  renderFoundationPreviewCompact(token, resolvedValue) {
    const label = token.label || this.describeFriendlyLabel(token.path);
    return `
      <div class="foundation-sidebar-preview">
        <div class="foundation-sidebar-preview-row">
          ${this.renderFoundationRowPreview(token, resolvedValue)}
          <div>
            <div><strong>${this.escapeHtml(label)}</strong></div>
            <div class="foundation-sidebar-preview-meta">${this.escapeHtml(resolvedValue || "Not set")}</div>
          </div>
        </div>
      </div>
    `;
  }

  renderFoundationPalettePicker(selectedAlias) {
    const groups = this.getPaletteOptionGroups();
    if (!groups.length) {
      return "";
    }

    return `
      <div class="foundation-palette-grid">
        ${groups.map((group) => `
          <div class="foundation-palette-row">
            <div class="foundation-palette-family">${this.escapeHtml(group.family)}</div>
            <div class="foundation-palette-swatches">
              ${group.items.map((option) => `
                <button
                  type="button"
                  class="foundation-palette-button ${selectedAlias === option.alias ? "selected" : ""}"
                  data-foundation-sidebar-palette-option="${this.escapeAttr(option.alias)}"
                  title="${this.escapeAttr(option.label || option.alias)}">
                  <span class="foundation-palette-chip" style="background:${this.escapeAttr(option.value || "#ffffff")};"></span>
                  <span class="foundation-palette-label">${this.escapeHtml(option.shade || "")}</span>
                </button>`).join("")}
            </div>
          </div>`).join("")}
      </div>
    `;
  }

  renderFoundationRowPreview(token, resolvedValue) {
    const type = String(token.type || "").toLowerCase();

    if (type === "color" && this.isColorLike(resolvedValue)) {
      return `<span class="foundation-swatch" style="background:${this.escapeAttr(resolvedValue)};"></span>`;
    }

    if (token.section === "radius") {
      return `<span class="foundation-preview-chip" style="border-radius:${this.escapeAttr(resolvedValue || "0px")};"></span>`;
    }

    if (token.section === "shadows") {
      const shadowStyle = this.getShadowPreviewStyle(token.value, resolvedValue);
      return `<span class="foundation-shadow-chip" style="${shadowStyle ? `box-shadow:${this.escapeAttr(shadowStyle)};` : ""}"></span>`;
    }

    if (type === "dimension" || token.section === "spacing" || token.section === "layout") {
      const width = Math.max(18, Math.min(this.extractNumericValue(resolvedValue) * 3, 42));
      return `<span class="foundation-mini-bar" style="width:${width}px;"></span>`;
    }

    if (type === "fontfamily") {
      return `<span class="foundation-badge" style="font-family:${this.escapeAttr(resolvedValue)};">Aa</span>`;
    }

    if (type === "number") {
      return `<span class="foundation-badge">${this.escapeHtml(resolvedValue || "0")}</span>`;
    }

    return `<span class="foundation-badge">${this.escapeHtml(String(token.type || "Token"))}</span>`;
  }

  renderFoundationSidebar() {
    const token = this.foundationSidebar?.draftToken;
    if (!token) {
      return "";
    }

    const label = token.label || this.describeFriendlyLabel(token.path);
    const title = this.foundationSidebar.tokenId
      ? `Edit ${token.section === "colours" ? "Colour" : "Token"} - ${label}`
      : this.foundationSidebar.mode === "palette-color"
        ? "Add Palette Colour"
        : this.foundationSidebar.mode === "custom-color"
          ? "Add Custom Colour"
          : "Add Spacing Token";
    const resolvedValue = this.summarizeFoundationTokenValue(token);
    const cssVariable = token.cssVariable || this.deriveCssVariable(token.path, token.meta);

    return `
      <div class="modal-backdrop">
        <div class="foundation-sidebar-shell">
          <div class="foundation-sidebar" role="dialog" aria-modal="true" aria-label="${this.escapeAttr(title)}">
            <div class="foundation-sidebar-layout">
              <div class="foundation-sidebar-header">
                <div>
                  <p class="eyebrow">${this.escapeHtml(token.section === "colours" ? "Foundation Tokens" : this.describeFriendlyLabel(token.section || "Foundation Tokens"))}</p>
                  <h2>${this.escapeHtml(title)}</h2>
                </div>
              </div>
              <div class="foundation-sidebar-body">
                <div class="foundation-field-grid">
                  ${this.renderFoundationSidebarFields(token)}
                </div>
                ${this.renderFoundationPreviewCompact(token, resolvedValue)}
                <details class="foundation-details foundation-disclosure">
                  <summary>Advanced details</summary>
                  <div class="token-detail-grid" style="margin-top:12px;">
                    ${this.renderMeta("Property alias", token.propertyAlias || "")}
                    ${this.renderMeta("Token path", token.path || "")}
                    ${this.renderMeta("CSS variable", cssVariable)}
                    ${this.renderMeta("Source", token.editorKind || token.section || "")}
                    ${this.renderMeta("Raw value", token.rawValue || this.formatTokenValue(token.value))}
                    ${this.renderMeta("Resolved value", resolvedValue)}
                  </div>
                </details>
              </div>
              <div class="foundation-sidebar-footer">
                <uui-button look="secondary" data-foundation-sidebar-close>Cancel</uui-button>
                <div>
                  ${token.removable ? `<uui-button look="outline" color="danger" data-foundation-sidebar-delete>Delete</uui-button>` : ""}
                </div>
                <div class="foundation-sidebar-footer-actions">
                  <uui-button look="primary" data-foundation-sidebar-save>Save</uui-button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  renderFoundationSidebarFields(token) {
    const summaryPath = token.path || "";

    if (token.editorKind === "paletteColor") {
      const options = Array.isArray(this.foundation?.paletteOptions) ? this.foundation.paletteOptions : [];
      return `
        <div class="field">
          <label for="foundation-sidebar-palette">Palette value</label>
          <select id="foundation-sidebar-palette" data-foundation-sidebar-palette>
            ${options.map((option) => `<option value="${this.escapeAttr(option.alias)}" ${token.paletteAlias === option.alias ? "selected" : ""}>${this.escapeHtml(option.label)}</option>`).join("")}
          </select>
          <div class="foundation-field-help">Choose a built-in palette colour.</div>
        </div>
        ${this.renderFoundationPalettePicker(token.paletteAlias)}
        ${this.renderValueDisplay("Token path preview", summaryPath)}
      `;
    }

    if (token.editorKind === "customColor") {
      const textValue = this.formatTokenValue(token.value);
      return `
        <div class="field">
          <label for="foundation-sidebar-label">Label</label>
          <input id="foundation-sidebar-label" type="text" value="${this.escapeAttr(token.label || "")}" data-foundation-sidebar-input="label" />
          <div class="foundation-field-help">Editor-facing name shown in the list.</div>
        </div>
        <div class="field">
          <label for="foundation-sidebar-color-name">Token name</label>
          <input id="foundation-sidebar-color-name" type="text" value="${this.escapeAttr(token.name || "")}" data-foundation-sidebar-input="name" />
          <div class="foundation-field-help">Used in the generated token path.</div>
        </div>
        <div class="field">
          <label for="foundation-sidebar-color-value">Colour value</label>
          <div class="token-input-row">
            ${this.isHexColor(textValue) ? `<input type="color" value="${this.escapeAttr(textValue)}" data-foundation-sidebar-input="value" />` : ""}
            <input id="foundation-sidebar-color-value" type="text" value="${this.escapeAttr(textValue)}" data-foundation-sidebar-input="value" placeholder="#0055ff" />
          </div>
        </div>
        ${this.renderValueDisplay("Token path preview", summaryPath)}
      `;
    }

    if (token.section === "spacing" && token.removable) {
      return `
        <div class="field">
          <label for="foundation-sidebar-spacing-label">Label</label>
          <input id="foundation-sidebar-spacing-label" type="text" value="${this.escapeAttr(token.label || "")}" data-foundation-sidebar-input="label" />
        </div>
        <div class="field">
          <label for="foundation-sidebar-spacing-name">Token name</label>
          <input id="foundation-sidebar-spacing-name" type="text" value="${this.escapeAttr(token.name || "")}" data-foundation-sidebar-input="name" />
        </div>
        ${this.renderFoundationResponsiveFields(token)}
        ${this.renderValueDisplay("Token path preview", summaryPath)}
      `;
    }

    if (token.type === "fontfamily") {
      return `
        <div class="field">
          <label for="foundation-sidebar-value">Value</label>
          <input id="foundation-sidebar-value" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-sidebar-input="value" />
        </div>
      `;
    }

    if (token.type === "number") {
      return `
        <div class="field">
          <label for="foundation-sidebar-value">Value</label>
          <input id="foundation-sidebar-value" type="number" step="any" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-sidebar-input="value" />
        </div>
      `;
    }

    if (token.section === "shadows" || token.editorKind === "shadowString") {
      return `
        <div class="field">
          <label for="foundation-sidebar-shadow">Shadow value</label>
          <input id="foundation-sidebar-shadow" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-sidebar-input="value" />
        </div>
      `;
    }

    if (token.value && typeof token.value === "object" && ["mobile", "tablet", "desktop"].some((key) => Object.prototype.hasOwnProperty.call(token.value, key))) {
      return this.renderFoundationResponsiveFields(token);
    }

    return `
      <div class="field">
        <label for="foundation-sidebar-value">Value</label>
        <input id="foundation-sidebar-value" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-sidebar-input="value" />
      </div>
    `;
  }

  renderFoundationResponsiveFields(token) {
    return `
      <div class="triple">
        ${["mobile", "tablet", "desktop"].map((breakpoint) => `
          <div class="field">
            <label for="foundation-sidebar-${breakpoint}">${this.escapeHtml(this.describeFriendlyLabel(breakpoint))}</label>
            <input
              id="foundation-sidebar-${breakpoint}"
              type="text"
              value="${this.escapeAttr(this.formatTokenValue(token.value?.[breakpoint] || ""))}"
              data-foundation-sidebar-responsive="${breakpoint}"
              placeholder="16px" />
          </div>`).join("")}
      </div>
    `;
  }

  renderSemanticRoles(parsed, tokenMetaMap) {
    return `
      <div class="stack">
        ${this.getSemanticRoles(parsed, tokenMetaMap).map((group) => `
          <details class="accordion" ${group.id === "page" ? "open" : ""}>
            <summary>
              <span>${this.escapeHtml(group.label)}</span>
              <span class="accordion-meta">${group.roles.length} value${group.roles.length === 1 ? "" : "s"}</span>
            </summary>
            <div class="accordion-body">
              <div class="token-grid">
                ${group.roles.map((role) => {
                  const options = this.getPrimitiveReferenceOptions(parsed, role.type);
                  const rawValue = role.token ? this.formatTokenValue(role.token.value) : "";
                  const resolvedValue = this.getResolvedDraftValue(parsed, role.path);
                  const currentReference = typeof role.token?.value === "string" && role.token.value.match(/^\{(.+)\}$/) ? role.token.value.slice(1, -1) : "";
                  const referencedLabel = currentReference ? this.describeFriendlyLabel(currentReference) : "";

                  return `
                    <article class="role-card">
                      <div class="token-head">
                        <div class="token-title">
                          <strong>${this.escapeHtml(role.label)}</strong>
                          <div class="token-path">${this.escapeHtml(role.path)}</div>
                        </div>
                      </div>
                      ${currentReference
                        ? `<div class="mapping-card"><strong>Uses ${this.escapeHtml(referencedLabel)}</strong><div>${this.escapeHtml(`{${currentReference}}`)}</div></div>`
                        : this.renderValueDisplay("Current value", rawValue || "Not set")}
                      <div class="editor-grid">
                        <label for="role-select-${this.escapeAttr(role.path)}">Foundation token reference</label>
                        <select id="role-select-${this.escapeAttr(role.path)}" data-role-reference="${this.escapeAttr(role.path)}" data-role-type="${this.escapeAttr(role.type)}">
                          <option value="">Direct value / none</option>
                          ${options.map((option) => `<option value="${this.escapeAttr(option.path)}" ${currentReference === option.path ? "selected" : ""}>${this.escapeHtml(option.path)}</option>`).join("")}
                        </select>
                      </div>
                      <div class="editor-grid">
                        <label for="role-direct-${this.escapeAttr(role.path)}">Direct value</label>
                        <div class="inline-row">
                          ${this.isHexColor(rawValue) ? `<input type="color" value="${this.escapeAttr(rawValue)}" data-role-color="${this.escapeAttr(role.path)}" data-role-type="${this.escapeAttr(role.type)}" />` : ""}
                          <input id="role-direct-${this.escapeAttr(role.path)}" type="text" value="${this.escapeAttr(rawValue)}" data-role-direct="${this.escapeAttr(role.path)}" data-role-type="${this.escapeAttr(role.type)}" placeholder="#0055ff or {color.brand.primary}" />
                        </div>
                      </div>
                      ${this.renderValueDisplay("Resolved value", resolvedValue || "Pending preview")}
                      ${this.renderTokenPreview(role.type, resolvedValue, role.token?.value)}
                    </article>
                  `;
                }).join("")}
              </div>
            </div>
          </details>`).join("")}
      </div>
    `;
  }

  renderSourceOverview(currentFeedback) {
    const summaries = Array.isArray(currentFeedback?.sourceSummaries) ? currentFeedback.sourceSummaries : [];

    if (!summaries.length) {
      const counts = new Map();
      (Array.isArray(this.tokens) ? this.tokens : []).forEach((token) => {
        const key = token.sourceType || "Unknown";
        counts.set(key, (counts.get(key) || 0) + 1);
      });

      if (!counts.size) {
        return `<p class="empty">Run Validate or Preview to inspect draft source layering.</p>`;
      }

      return `
        <div class="source-grid">
          ${Array.from(counts.entries()).map(([sourceType, count]) => `
            <div class="source-row">
              <div class="source-name"><strong>${this.escapeHtml(sourceType)}</strong><div>Winning tokens in active build</div></div>
              <div><strong>${this.escapeHtml(String(count))}</strong><div class="small-note">Count</div></div>
              <div><strong>n/a</strong><div class="small-note">Before</div></div>
              <div><strong>n/a</strong><div class="small-note">After</div></div>
              <div><strong>Info only</strong><div class="small-note">Overrides</div></div>
            </div>`).join("")}
        </div>
      `;
    }

    return `
      <div class="source-grid">
        ${summaries.map((item) => `
          <div class="source-row">
            <div class="source-name">
              <strong>${this.escapeHtml(item.sourceType)}</strong>
              <div>${this.escapeHtml(item.sourceName || "Unnamed source")}</div>
            </div>
            <div><strong>${this.escapeHtml(String(item.tokenCountBeforeMerge || 0))}</strong><div class="small-note">Before merge</div></div>
            <div><strong>${this.escapeHtml(String(item.tokenCountAfterMerge || 0))}</strong><div class="small-note">Winning tokens</div></div>
            <div><strong>${this.escapeHtml(String(item.tokensOverriddenByHigherSource || 0))}</strong><div class="small-note">Lower-priority overrides</div></div>
            <div><strong>${item.enabled ? "Enabled" : "Disabled"}</strong><div class="small-note">State</div></div>
          </div>`).join("")}
      </div>
    `;
  }

  renderAdvanced(parsed, active, currentFeedback, errors, warnings, infos, filteredTokens, generatedCss, tailwindJson) {
    return `
      <div class="grid">
        <div class="stack">
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Raw JSON</p>
                <h2>Import, export, and direct editing</h2>
              </div>
            </div>
            <div class="field">
              <label for="doc-name">Draft name</label>
              <input id="doc-name" type="text" value="${this.escapeAttr(this.editorName)}" placeholder="Spring campaign theme" />
            </div>
            <div class="field">
              <label for="doc-file">Import token JSON</label>
              <input id="doc-file" type="file" accept=".json,application/json" />
            </div>
            <div class="field">
              <label for="doc-json">Raw token JSON</label>
              <textarea id="doc-json" spellcheck="false">${this.escapeHtml(this.editorJson)}</textarea>
              <div class="toolbar">
                <select id="picker-context-mode">
                  ${PICKER_CONTEXT_OPTIONS.map((item) => `<option value="${this.escapeAttr(item.value)}" ${this.picker.contextMode === item.value ? "selected" : ""}>${this.escapeHtml(item.label)}</option>`).join("")}
                </select>
                <button class="subtle" data-action="open-picker">Insert token reference</button>
                <button class="subtle" data-action="export" ${this.loading.export ? "disabled" : ""}>${this.loading.export ? "Exporting..." : "Export Active JSON"}</button>
              </div>
            </div>
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Generated CSS</p>
                <h2>Draft output snapshot</h2>
              </div>
            </div>
            ${generatedCss ? `<pre>${this.escapeHtml(generatedCss)}</pre>` : `<p class="empty">Run Validate or Preview to inspect generated CSS.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Tailwind Output</p>
                <h2>Extended theme export</h2>
              </div>
            </div>
            ${tailwindJson ? `<pre>${this.escapeHtml(tailwindJson)}</pre>` : `<p class="empty">Run Validate or Preview to inspect Tailwind output.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Source Trace</p>
                <h2>Active token registry</h2>
              </div>
              <span class="pill">${filteredTokens.length} / ${this.tokens.length} tokens</span>
            </div>
            ${this.renderTokenFilters()}
            ${filteredTokens.length ? `
              <div class="table-wrap"><table>
                <thead>
                  <tr>
                    <th>Path</th>
                    <th>Type</th>
                    <th>Source</th>
                    <th>Priority</th>
                    <th>CSS variable</th>
                    <th>Value</th>
                  </tr>
                </thead>
                <tbody>
                  ${filteredTokens.map((token) => `
                    <tr data-token-path="${this.escapeAttr(token.path)}" class="${this.selectedToken?.path === token.path ? "active" : ""}">
                      <td>${this.escapeHtml(token.path)}</td>
                      <td>${this.escapeHtml(token.type)}</td>
                      <td>${this.escapeHtml(token.source || token.sourceType || "")}</td>
                      <td>${this.escapeHtml(String(token.sourcePriority ?? ""))}</td>
                      <td>${this.escapeHtml(token.generatedCssVariable || "")}</td>
                      <td>${this.escapeHtml(token.valuePreview || "")}</td>
                    </tr>`).join("")}
                </tbody>
              </table></div>
            ` : `<p class="empty">No active build token list available.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Merge Log</p>
                <h2>Developer merge events</h2>
              </div>
            </div>
            ${Array.isArray(currentFeedback?.mergeEvents) && currentFeedback.mergeEvents.length ? this.renderMergeLog(currentFeedback.mergeEvents) : `<p class="empty">Run Validate or Preview to inspect merge events.</p>`}
          </section>
        </div>
        <div class="stack">
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Diagnostics</p>
                <h2>Blocking issues first</h2>
              </div>
              <span class="pill">${this.countDiagnostics(currentFeedback?.errors || [])} error${this.countDiagnostics(currentFeedback?.errors || []) === 1 ? "" : "s"}</span>
            </div>
            ${Object.keys(errors).length ? Object.entries(errors).map(([stage, items]) => this.renderErrorGroup(stage, items)).join("") : `<p class="empty">No current errors.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Warnings</p>
                <h2>Non-blocking feedback</h2>
              </div>
              <span class="pill">${warnings.length} warning${warnings.length === 1 ? "" : "s"}</span>
            </div>
            ${warnings.length ? `<ul class="warning-list">${warnings.map((item) => `<li><strong>${this.escapeHtml(item.tokenPath || "General")}</strong>: ${this.escapeHtml(item.message)}</li>`).join("")}</ul>` : `<p class="empty">No current warnings.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Info</p>
                <h2>Trace and developer notes</h2>
              </div>
              <span class="pill">${infos.length} info</span>
            </div>
            ${infos.length ? `<ul class="warning-list">${infos.map((item) => `<li><strong>${this.escapeHtml(item.tokenPath || "General")}</strong>: ${this.escapeHtml(item.message)}</li>`).join("")}</ul>` : `<p class="empty">No current info items.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Selected Token</p>
                <h2>Detail trace</h2>
              </div>
              ${this.loading.token ? `<span class="pill">Loading</span>` : ""}
            </div>
            ${this.selectedToken ? this.renderTokenDetail(this.selectedToken) : `<p class="empty">Select a token from the source trace table.</p>`}
          </section>
          <section class="panel">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Build Snapshot</p>
                <h2>Current document state</h2>
              </div>
            </div>
            ${active ? `
              <div class="meta-grid">
                ${this.renderMeta("Document", active.name)}
                ${this.renderMeta("Status", active.status)}
                ${this.renderMeta("Created", this.formatDate(active.createdDateUtc))}
                ${this.renderMeta("Updated", this.formatDate(active.updatedDateUtc))}
                ${this.renderMeta("Updated by", active.updatedBy || this.status?.latestBuildUpdatedBy || "")}
                ${this.renderMeta("Token count", String(active.tokenCount || 0))}
                ${this.renderMeta("CSS path", active.generatedCssPath || "")}
                ${this.renderMeta("Tailwind path", active.generatedTailwindPath || "")}
              </div>` : `<p class="empty">No active token document.</p>`}
          </section>
        </div>
      </div>
    `;
  }

  renderPreviewContent() {
    return `
      <div class="preview-grid">
        <div>
          <div class="preview-actions" style="justify-content:space-between; align-items:center; margin-bottom:12px;">
            <strong>Studio Notes</strong>
            <a class="preview-link" href="#">View details</a>
          </div>
          <h2 class="preview-heading">Shape the look of your next page</h2>
          <p class="preview-body">Paragraph copy shows body text, muted text, and link styling together so colour, type, and spacing changes are easy to spot. <a class="preview-link" href="#">Preview link</a>.</p>
        </div>
        <div class="preview-actions">
          <button class="preview-btn preview-btn-primary">Primary button</button>
          <button class="preview-btn preview-btn-secondary">Secondary button</button>
        </div>
        <article class="preview-card">
          <h4>Card preview</h4>
          <p class="preview-body">Surface, border, radius, and shadow update from draft values only.</p>
        </article>
        <div class="preview-form">
          <input class="preview-input" type="text" value="Form input preview" />
          <input class="preview-input preview-input-focus" type="text" value="Focus ring preview" />
        </div>
        <div class="preview-alert">
          Alert / feedback block using your draft colours and text styles.
        </div>
        <div class="preview-spacing">
          <div class="preview-spacing-box preview-space-sm"></div>
          <div class="preview-spacing-box preview-space-md"></div>
          <div class="preview-spacing-box preview-space-lg"></div>
        </div>
      </div>
    `;
  }

  renderComparison(comparison) {
    return `
      <div class="meta-grid">
        ${this.renderMeta("Changed", String(comparison.changedTokenCount || 0))}
        ${this.renderMeta("Added", String(comparison.addedTokenCount || 0))}
        ${this.renderMeta("Removed", String(comparison.removedTokenCount || 0))}
        ${this.renderMeta("Live CSS", "Unaffected")}
      </div>
      ${this.renderChangeList("Changed tokens", comparison.changedTokens || [])}
      ${this.renderChangeList("Added tokens", comparison.addedTokens || [])}
      ${this.renderChangeList("Removed tokens", comparison.removedTokens || [])}
    `;
  }

  renderChangeList(label, items) {
    if (!Array.isArray(items) || !items.length) {
      return "";
    }

    return `
      <div class="detail-card">
        <strong>${this.escapeHtml(label)}</strong>
        <ul class="change-list">
          ${items.slice(0, 8).map((item) => `<li><strong>${this.escapeHtml(item.path)}</strong>: ${this.escapeHtml(item.activeValue || "none")} -> ${this.escapeHtml(item.draftValue || "none")}</li>`).join("")}
        </ul>
      </div>
    `;
  }

  renderPickerModal() {
    const activeContext = this.getActivePickerContext();
    return `
      <div class="modal-backdrop">
        <div class="modal">
          <div class="panel-head">
            <div>
              <p class="eyebrow">Reference Picker</p>
              <h2>Insert token reference</h2>
              <p class="small-note">Picker inserts only <code>{token.path}</code>. No raw values. No CSS variable names.</p>
            </div>
            <button class="subtle" data-action="close-picker">Close</button>
          </div>
          <div class="stack">
            <div class="picker-controls">
              <input id="picker-query" type="text" value="${this.escapeAttr(this.picker.query)}" placeholder="Search token path, type, source" />
              <select id="picker-token-type">
                ${PICKER_TOKEN_TYPE_OPTIONS.map((item) => `<option value="${this.escapeAttr(item.value)}" ${this.picker.tokenType === item.value ? "selected" : ""}>${this.escapeHtml(item.label)}</option>`).join("")}
              </select>
              <select id="picker-context">
                ${PICKER_CONTEXT_OPTIONS.map((item) => `<option value="${this.escapeAttr(item.value)}" ${this.picker.contextMode === item.value ? "selected" : ""}>${this.escapeHtml(item.label)}</option>`).join("")}
              </select>
            </div>
            <div class="small-note">Detected context: ${this.escapeHtml(this.picker.detectedContext || "none")} | Applied context: ${this.escapeHtml(activeContext || "none")} | Applied token type: ${this.escapeHtml(this.picker.appliedTokenType || "none")}</div>
            ${this.loading.picker ? `<span class="pill">Loading picker</span>` : ""}
            ${!this.picker.hasActiveBuild ? `<div class="notice">${this.escapeHtml(this.picker.emptyMessage || "No active design token build is available. Validate and activate a token document first.")}</div>` : ""}
            ${this.picker.hasActiveBuild && this.picker.items.length ? `
              <div class="picker-results">
                ${this.picker.items.map((item) => `
                  <div class="picker-item">
                    <div>
                      <div class="picker-path">${this.escapeHtml(item.path)}</div>
                      <div class="picker-preview">${this.escapeHtml(item.referenceValue)} | ${this.escapeHtml(item.cssVariableName || "")}</div>
                    </div>
                    <div>${this.escapeHtml(item.type)}</div>
                    <div>${this.escapeHtml(item.resolvedValuePreview || item.sourceName || "")}</div>
                    <div><button class="primary" data-picker-insert="${this.escapeAttr(item.referenceValue)}">Insert</button></div>
                  </div>`).join("")}
              </div>` : ""}
            ${this.picker.hasActiveBuild && !this.picker.items.length && this.picker.emptyMessage ? `<p class="empty">${this.escapeHtml(this.picker.emptyMessage)}</p>` : ""}
          </div>
        </div>
      </div>
    `;
  }

  renderTokenEditor(token) {
    const type = String(token.type || "").toLowerCase();
    const pathAttr = this.escapeAttr(token.path);
    const typeAttr = this.escapeAttr(token.type);
    const sectionAttr = this.escapeAttr(token.section || "");
    const tokenAttr = this.escapeAttr(token.id || "");

    if (token.editorKind === "paletteColor") {
      const options = Array.isArray(this.foundation?.paletteOptions) ? this.foundation.paletteOptions : [];
      return `
        <div class="editor-grid">
          <label for="token-${tokenAttr}-palette">Palette</label>
          <select id="token-${tokenAttr}-palette" data-foundation-color-palette="${tokenAttr}" data-foundation-section="${sectionAttr}">
            ${options.map((option) => `<option value="${this.escapeAttr(option.alias)}" ${token.paletteAlias === option.alias ? "selected" : ""}>${this.escapeHtml(option.label)}</option>`).join("")}
          </select>
        </div>
      `;
    }

    if (token.editorKind === "customColor") {
      return `
        <div class="editor-grid compact">
          <div class="editor-grid">
            <label for="token-${tokenAttr}-name">Name</label>
            <input id="token-${tokenAttr}-name" type="text" value="${this.escapeAttr(token.name || "")}" data-foundation-color-name="${tokenAttr}" data-foundation-section="${sectionAttr}" />
          </div>
          <div class="editor-grid">
            <label for="token-${tokenAttr}-value">Value</label>
            <div class="token-input-row">
              ${this.isHexColor(this.formatTokenValue(token.value)) ? `<input type="color" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-color-value="${tokenAttr}" data-foundation-section="${sectionAttr}" />` : ""}
              <input id="token-${tokenAttr}-value" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-color-value="${tokenAttr}" data-foundation-section="${sectionAttr}" placeholder="#0055ff" />
            </div>
          </div>
        </div>
      `;
    }

    if (token.editorKind === "shadowString") {
      return `
        <div class="editor-grid">
          <input id="token-${tokenAttr}" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
      `;
    }

    if (type === "color") {
      const textValue = typeof token.value === "string" ? token.value : "";
      return `
        <div class="token-input-row">
            ${this.isHexColor(textValue) ? `<input type="color" value="${this.escapeAttr(textValue)}" data-token-direct="${pathAttr}" data-token-type="${typeAttr}" />` : ""}
            <input id="token-${pathAttr}" type="text" value="${this.escapeAttr(textValue)}" data-token-direct="${pathAttr}" data-token-type="${typeAttr}" placeholder="#0055ff or {color.brand.primary}" />
        </div>
      `;
    }

    if (type === "fontfamily") {
      return `
        <div class="editor-grid">
          <input id="token-${pathAttr}" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
      `;
    }

    if (type === "fontweight" || type === "number") {
      return `
        <div class="editor-grid">
          <input id="token-${pathAttr}" type="number" step="any" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
      `;
    }

    if (type === "dimension" || type === "duration") {
      return this.renderDimensionEditor(token);
    }

    if (type === "typography") {
      return this.renderTypographyEditor(token);
    }

    if (type === "shadow") {
      return this.renderShadowEditor(token);
    }

    if (type === "border") {
      return this.renderBorderEditor(token);
    }

    return `
      <div class="editor-grid">
        <input id="token-${pathAttr}" type="text" value="${this.escapeAttr(this.formatTokenValue(token.value))}" data-token-direct="${pathAttr}" data-token-type="${typeAttr}" />
      </div>
    `;
  }

  renderDimensionEditor(token) {
    const value = token.value;
    const pathAttr = this.escapeAttr(token.path);
    const typeAttr = this.escapeAttr(token.type);

    if (typeof value === "string") {
      return `
        <div class="editor-grid">
          <input id="token-${pathAttr}" type="text" value="${this.escapeAttr(value)}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
      `;
    }

    if (value && typeof value === "object" && ["mobile", "tablet", "desktop"].some((key) => Object.prototype.hasOwnProperty.call(value, key))) {
      return `
        <div class="triple">
            ${["mobile", "tablet", "desktop"].map((breakpoint) => `
              <div class="editor-grid">
                <label for="${pathAttr}-${breakpoint}">${this.escapeHtml(this.describeFriendlyLabel(breakpoint))}</label>
                <input id="${pathAttr}-${breakpoint}" type="text" value="${this.escapeAttr(this.formatTokenValue(value[breakpoint] || ""))}" data-foundation-responsive="${tokenAttr}" data-foundation-section="${sectionAttr}" data-breakpoint="${breakpoint}" placeholder="16px" />
              </div>`).join("")}
        </div>
      `;
    }

    return `
      <div class="editor-grid compact">
        <div class="editor-grid">
          <label for="${pathAttr}-value">Value</label>
          <input id="${pathAttr}-value" type="text" value="${this.escapeAttr(String(value?.value ?? ""))}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
        <div class="editor-grid">
          <label for="${pathAttr}-unit">Unit</label>
          <input id="${pathAttr}-unit" type="text" value="${this.escapeAttr(value?.unit ?? "")}" data-foundation-simple="${tokenAttr}" data-foundation-section="${sectionAttr}" />
        </div>
      </div>
    `;
  }

  renderTypographyEditor(token) {
    const value = token.value && typeof token.value === "object" ? token.value : {};
    const pathAttr = this.escapeAttr(token.path);
    const typeAttr = this.escapeAttr(token.type);
    return `
      <div class="editor-grid compact">
          <div class="editor-grid">
            <label for="${pathAttr}-fontFamily">Font family</label>
            <input id="${pathAttr}-fontFamily" type="text" value="${this.escapeAttr(this.formatTokenValue(value.fontFamily || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="fontFamily" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-fontWeight">Font weight</label>
            <input id="${pathAttr}-fontWeight" type="text" value="${this.escapeAttr(this.formatTokenValue(value.fontWeight || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="fontWeight" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-fontSize">Font size</label>
            <input id="${pathAttr}-fontSize" type="text" value="${this.escapeAttr(this.formatTokenValue(value.fontSize || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="fontSize" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-lineHeight">Line height</label>
            <input id="${pathAttr}-lineHeight" type="text" value="${this.escapeAttr(this.formatTokenValue(value.lineHeight || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="lineHeight" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-letterSpacing">Letter spacing</label>
            <input id="${pathAttr}-letterSpacing" type="text" value="${this.escapeAttr(this.formatTokenValue(value.letterSpacing || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="letterSpacing" />
          </div>
      </div>
    `;
  }

  renderShadowEditor(token) {
    const value = token.value && typeof token.value === "object" ? token.value : {};
    const pathAttr = this.escapeAttr(token.path);
    const typeAttr = this.escapeAttr(token.type);
    return `
      <div class="editor-grid compact">
          <div class="editor-grid">
            <label for="${pathAttr}-color">Color</label>
            <input id="${pathAttr}-color" type="text" value="${this.escapeAttr(this.formatTokenValue(value.color || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="color" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-offsetX">Offset X</label>
            <input id="${pathAttr}-offsetX" type="text" value="${this.escapeAttr(this.formatTokenValue(value.offsetX || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="offsetX" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-offsetY">Offset Y</label>
            <input id="${pathAttr}-offsetY" type="text" value="${this.escapeAttr(this.formatTokenValue(value.offsetY || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="offsetY" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-blur">Blur</label>
            <input id="${pathAttr}-blur" type="text" value="${this.escapeAttr(this.formatTokenValue(value.blur || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="blur" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-spread">Spread</label>
            <input id="${pathAttr}-spread" type="text" value="${this.escapeAttr(this.formatTokenValue(value.spread || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="spread" />
          </div>
      </div>
    `;
  }

  renderBorderEditor(token) {
    const value = token.value && typeof token.value === "object" ? token.value : {};
    const pathAttr = this.escapeAttr(token.path);
    const typeAttr = this.escapeAttr(token.type);
    return `
      <div class="editor-grid compact">
          <div class="editor-grid">
            <label for="${pathAttr}-width">Width</label>
            <input id="${pathAttr}-width" type="text" value="${this.escapeAttr(this.formatTokenValue(value.width || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="width" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-style">Style</label>
            <input id="${pathAttr}-style" type="text" value="${this.escapeAttr(this.formatTokenValue(value.style || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="style" />
          </div>
          <div class="editor-grid">
            <label for="${pathAttr}-color">Color</label>
            <input id="${pathAttr}-color" type="text" value="${this.escapeAttr(this.formatTokenValue(value.color || ""))}" data-token-sub="${pathAttr}" data-token-type="${typeAttr}" data-sub="color" />
          </div>
      </div>
    `;
  }

  renderTokenPreview(type, resolvedValue, rawValue = null) {
    const normalizedType = String(type || "").toLowerCase();

    if (!resolvedValue) {
      return "";
    }

    if (normalizedType === "color") {
      return `
        <div class="token-preview-visual">
          ${this.isColorLike(resolvedValue) ? `<span class="token-swatch-large" style="background:${this.escapeAttr(resolvedValue)};"></span>` : ""}
        </div>
      `;
    }

    if (normalizedType === "dimension" || normalizedType === "duration") {
      const numeric = this.extractNumericValue(resolvedValue);
      const isRadius = String(resolvedValue || "").includes("px") && numeric > 0 && numeric <= 64;
      return `
        <div class="token-preview-visual">
          <div class="size-preview">
            ${numeric > 0 ? `<span class="size-bar" style="width:${Math.max(24, Math.min(numeric * 3, 220))}px;"></span>` : ""}
            ${isRadius ? `<span class="radius-preview" style="border-radius:${this.escapeAttr(resolvedValue)};"></span>` : ""}
          </div>
        </div>
      `;
    }

    if (normalizedType === "fontfamily" || normalizedType === "fontweight" || normalizedType === "typography") {
      const typographyStyles = normalizedType === "typography" && rawValue && typeof rawValue === "object"
        ? `font-family:${this.escapeAttr(this.formatTokenValue(rawValue.fontFamily || ""))};font-weight:${this.escapeAttr(this.formatTokenValue(rawValue.fontWeight || ""))};font-size:${this.escapeAttr(this.formatTokenValue(rawValue.fontSize || ""))};line-height:${this.escapeAttr(this.formatTokenValue(rawValue.lineHeight || ""))};`
        : "";
      return `
        <div class="token-preview-visual">
          <div class="token-sample" style="${normalizedType === "fontfamily" ? `font-family:${resolvedValue};` : ""}${normalizedType === "fontweight" ? `font-weight:${resolvedValue};` : ""}${typographyStyles}">
            The quick brown fox jumps over the lazy dog.
          </div>
        </div>
      `;
    }

    if (normalizedType === "shadow") {
      const shadowStyle = this.getShadowPreviewStyle(rawValue, resolvedValue);
      return `
        <div class="token-preview-visual">
          ${shadowStyle ? `<span class="token-shadow-sample" style="box-shadow:${this.escapeAttr(shadowStyle)};"></span>` : ""}
        </div>
      `;
    }

    return "";
  }

  renderValueDisplay(label, value) {
    return `<div class="meta-card"><strong>${this.escapeHtml(label)}</strong><div>${this.escapeHtml(value || "") || '<span class="empty">None</span>'}</div></div>`;
  }

  getShadowPreviewStyle(rawValue, resolvedValue) {
    if (typeof rawValue === "string" && rawValue.trim()) {
      return rawValue.trim();
    }

    if (rawValue && typeof rawValue === "object" && !Array.isArray(rawValue)) {
      const color = this.formatTokenValue(rawValue.color || "#00000022");
      const offsetX = this.formatTokenValue(rawValue.offsetX || "0px");
      const offsetY = this.formatTokenValue(rawValue.offsetY || "4px");
      const blur = this.formatTokenValue(rawValue.blur || "12px");
      const spread = this.formatTokenValue(rawValue.spread || "0px");
      return `${offsetX} ${offsetY} ${blur} ${spread} ${color}`;
    }

    const parts = String(resolvedValue || "")
      .split("|")
      .map((item) => item.trim())
      .reduce((acc, part) => {
        const [key, ...rest] = part.split(":");
        if (key && rest.length) {
          acc[key.trim()] = rest.join(":").trim();
        }
        return acc;
      }, {});

    if (!Object.keys(parts).length) {
      return "";
    }

    return `${parts.offsetX || "0px"} ${parts.offsetY || "4px"} ${parts.blur || "12px"} ${parts.spread || "0px"} ${parts.color || "#00000022"}`;
  }

  bindEvents() {
    this.shadowRoot.querySelectorAll("[data-tab]").forEach((button) => {
      button.addEventListener("click", () => {
        this.activeTab = button.getAttribute("data-tab") || "workbench";
        this.render();
      });
    });

    this.shadowRoot.getElementById("doc-name")?.addEventListener("input", (event) => {
      this.editorName = event.target.value;
    });

    this.shadowRoot.getElementById("doc-json")?.addEventListener("input", (event) => {
      this.editorJson = event.target.value;
    });

    this.shadowRoot.getElementById("doc-json")?.addEventListener("click", (event) => {
      this.picker.detectedContext = this.detectJsonFieldContext(event.target.value, event.target.selectionStart || 0);
      this.render();
    });

    const importFileInput = this.shadowRoot.getElementById("doc-file");
    const topImportFileInput = this.shadowRoot.getElementById("top-import-file");

    importFileInput?.addEventListener("change", async (event) => {
      await this.handleImportFile(event.target.files?.[0] || null);
    });

    topImportFileInput?.addEventListener("change", async (event) => {
      await this.handleImportFile(event.target.files?.[0] || null);
    });

    this.shadowRoot.querySelectorAll("[data-theme-variant]").forEach((input) => {
      input.addEventListener("change", (event) => {
        this.updateThemeVariantSelection(event.target.getAttribute("data-theme-variant"), event.target.checked);
      });
    });

    this.shadowRoot.querySelector('[data-action="preview"]')?.addEventListener("click", () => this.handlePreview());
    this.shadowRoot.querySelector('[data-action="validate"]')?.addEventListener("click", () => this.handleValidate());
    this.shadowRoot.querySelector('[data-action="save"]')?.addEventListener("click", () => this.handleSaveDraft());
    this.shadowRoot.querySelector('[data-action="activate"]')?.addEventListener("click", () => this.handleActivate());
    this.shadowRoot.querySelector('[data-action="export"]')?.addEventListener("click", () => this.handleExport());
    this.shadowRoot.querySelector('[data-action="rebuild"]')?.addEventListener("click", () => this.handleRebuild());
    this.shadowRoot.querySelector('[data-action="open-picker"]')?.addEventListener("click", () => this.openPicker());
    this.shadowRoot.querySelector('[data-action="close-picker"]')?.addEventListener("click", () => this.closePicker());
    this.shadowRoot.querySelector('[data-action="preview-theme"]')?.addEventListener("click", async () => {
      this.activeTab = "preview";
      this.render();
      await this.handlePreview();
    });
    this.shadowRoot.querySelector('[data-action="import-theme"]')?.addEventListener("click", () => {
      topImportFileInput?.click();
    });

    this.shadowRoot.querySelectorAll("[data-token-direct]").forEach((input) => {
      input.addEventListener("change", (event) => {
        this.commitTokenValue(
          event.target.getAttribute("data-token-direct"),
          event.target.getAttribute("data-token-type"),
          this.coerceSimpleValue(event.target.value, event.target.getAttribute("data-token-type")),
        );
      });
    });

    this.shadowRoot.querySelectorAll("[data-token-part]").forEach((input) => {
      input.addEventListener("change", (event) => {
        const path = event.target.getAttribute("data-token-part");
        const type = event.target.getAttribute("data-token-type");
        const part = event.target.getAttribute("data-part");
        const existing = this.findDraftToken(this.tryParseEditorJson() || {}, path)?.value || {};
        const nextValue = {
          value: part === "value" ? this.coerceNumericInput(event.target.value) : existing.value ?? 0,
          unit: part === "unit" ? event.target.value : existing.unit ?? "",
        };
        this.commitTokenValue(path, type, nextValue);
      });
    });

    this.shadowRoot.querySelectorAll("[data-token-breakpoint]").forEach((input) => {
      input.addEventListener("change", (event) => {
        const path = event.target.getAttribute("data-token-breakpoint");
        const type = event.target.getAttribute("data-token-type");
        const breakpoint = event.target.getAttribute("data-breakpoint");
        const existing = this.findDraftToken(this.tryParseEditorJson() || {}, path)?.value || {};
        const nextValue = {
          mobile: existing.mobile || { value: 0, unit: "px" },
          tablet: existing.tablet || { value: 0, unit: "px" },
          desktop: existing.desktop || { value: 0, unit: "px" },
        };
        nextValue[breakpoint] = this.parseDimensionLike(event.target.value);
        this.commitTokenValue(path, type, nextValue);
      });
    });

    this.shadowRoot.querySelectorAll("[data-token-sub]").forEach((input) => {
      input.addEventListener("change", (event) => {
        this.commitTokenSubValue(
          event.target.getAttribute("data-token-sub"),
          event.target.getAttribute("data-token-type"),
          event.target.getAttribute("data-sub"),
          this.parseStructuredField(event.target.value),
        );
      });
    });

    this.shadowRoot.querySelectorAll("[data-foundation-open]").forEach((button) => {
      button.addEventListener("click", () => {
        const sectionId = button.getAttribute("data-foundation-section");
        const tokenId = button.getAttribute("data-foundation-open");
        this.openFoundationSidebar("edit", sectionId, tokenId);
      });
    });

    this.shadowRoot.querySelectorAll("[data-foundation-add]").forEach((button) => {
      button.addEventListener("click", () => {
        const action = button.getAttribute("data-foundation-add");
        const sectionId = action === "spacing" ? "spacing" : "colours";
        this.openFoundationSidebar(action, sectionId);
      });
    });

    this.shadowRoot.querySelectorAll("[data-foundation-sidebar-close]").forEach((button) => {
      button.addEventListener("click", () => this.closeFoundationSidebar());
    });

    this.shadowRoot.querySelector("[data-foundation-sidebar-save]")?.addEventListener("click", () => {
      this.saveFoundationSidebar();
    });

    this.shadowRoot.querySelector("[data-foundation-sidebar-delete]")?.addEventListener("click", () => {
      this.deleteFoundationSidebarToken();
    });

    this.shadowRoot.querySelectorAll("[data-foundation-sidebar-input]").forEach((input) => {
      input.addEventListener("change", (event) => {
        const field = event.target.getAttribute("data-foundation-sidebar-input");
        const nextValue = event.target.type === "number" ? Number(event.target.value) : event.target.value;
        this.updateFoundationSidebarDraft((token) => {
          token[field] = nextValue;
        });
      });
    });

    this.shadowRoot.querySelector("[data-foundation-sidebar-palette]")?.addEventListener("change", (event) => {
      this.updateFoundationSidebarDraft((token) => {
        token.paletteAlias = event.target.value;
      });
    });

    this.shadowRoot.querySelectorAll("[data-foundation-sidebar-palette-option]").forEach((button) => {
      button.addEventListener("click", () => {
        this.updateFoundationSidebarDraft((token) => {
          token.paletteAlias = button.getAttribute("data-foundation-sidebar-palette-option");
        });
      });
    });

    this.shadowRoot.querySelectorAll("[data-foundation-sidebar-responsive]").forEach((input) => {
      input.addEventListener("change", (event) => {
        const breakpoint = event.target.getAttribute("data-foundation-sidebar-responsive");
        this.updateFoundationSidebarDraft((token) => {
          if (!token.value || typeof token.value !== "object" || Array.isArray(token.value)) {
            token.value = { mobile: "", tablet: "", desktop: "" };
          }
          token.value[breakpoint] = event.target.value;
        });
      });
    });

    this.shadowRoot.querySelectorAll("[data-role-reference]").forEach((select) => {
      select.addEventListener("change", (event) => {
        const path = event.target.getAttribute("data-role-reference");
        const type = event.target.getAttribute("data-role-type");
        const value = event.target.value ? `{${event.target.value}}` : "";
        this.commitTokenValue(path, type, value || null);
      });
    });

    this.shadowRoot.querySelectorAll("[data-role-direct]").forEach((input) => {
      input.addEventListener("change", (event) => {
        this.commitTokenValue(
          event.target.getAttribute("data-role-direct"),
          event.target.getAttribute("data-role-type"),
          event.target.value,
        );
      });
    });

    this.shadowRoot.querySelectorAll("[data-role-color]").forEach((input) => {
      input.addEventListener("change", (event) => {
        this.commitTokenValue(
          event.target.getAttribute("data-role-color"),
          event.target.getAttribute("data-role-type"),
          event.target.value,
        );
      });
    });

    this.shadowRoot.querySelectorAll("[data-token-path]").forEach((row) => {
      row.addEventListener("click", () => this.handleTokenSelect(row.getAttribute("data-token-path")));
    });

    this.shadowRoot.querySelectorAll("[data-token-source-filter]").forEach((button) => {
      button.addEventListener("click", () => {
        this.tokenFilters.sourceType = button.getAttribute("data-token-source-filter") || "all";
        this.render();
      });
    });

    this.shadowRoot.querySelectorAll("[data-token-flag-filter]").forEach((button) => {
      button.addEventListener("click", () => {
        this.tokenFilters.flag = button.getAttribute("data-token-flag-filter") || "all";
        this.render();
      });
    });

    this.shadowRoot.querySelectorAll("[data-preview-theme]").forEach((button) => {
      button.addEventListener("click", () => {
        this.preview.theme = button.getAttribute("data-preview-theme") || "default";
        this.render();
      });
    });

    this.shadowRoot.getElementById("picker-query")?.addEventListener("input", async (event) => {
      this.picker.query = event.target.value;
      await this.loadPicker();
    });

    this.shadowRoot.getElementById("picker-token-type")?.addEventListener("change", async (event) => {
      this.picker.tokenType = event.target.value;
      await this.loadPicker();
    });

    this.shadowRoot.getElementById("picker-context")?.addEventListener("change", async (event) => {
      this.picker.contextMode = event.target.value;
      await this.loadPicker();
    });

    this.shadowRoot.querySelectorAll("[data-picker-insert]").forEach((button) => {
      button.addEventListener("click", () => this.insertPickerItem(button.getAttribute("data-picker-insert")));
    });
  }

  coerceSimpleValue(value, type) {
    const normalizedType = String(type || "").toLowerCase();
    if (normalizedType === "fontweight" || normalizedType === "number") {
      return this.coerceNumericInput(value);
    }

    return value;
  }

  coerceNumericInput(value) {
    if (String(value ?? "").trim() === "") {
      return "";
    }

    const number = Number(value);
    return Number.isFinite(number) ? number : value;
  }

  parseDimensionLike(value) {
    const trimmed = String(value || "").trim();
    const match = trimmed.match(/^(-?\d+(?:\.\d+)?)([a-z%]*)$/i);
    if (!match) {
      return trimmed;
    }

    return {
      value: Number(match[1]),
      unit: match[2] || "",
    };
  }

  parseStructuredField(value) {
    const trimmed = String(value || "").trim();
    if (!trimmed) {
      return "";
    }

    if (/^\{.+\}$/.test(trimmed)) {
      return trimmed;
    }

    if (/^-?\d+(?:\.\d+)?[a-z%]*$/i.test(trimmed)) {
      return this.parseDimensionLike(trimmed);
    }

    if (/^-?\d+(?:\.\d+)?$/i.test(trimmed)) {
      return Number(trimmed);
    }

    return trimmed;
  }

  renderErrorGroup(stage, items) {
    return `
      <div class="diag-group">
        <div class="diag-head">${this.escapeHtml(stage)} (${items.length})</div>
        <div class="diag-list">
          ${items.map((item) => `
            <div class="diag-item">
              <small>${this.escapeHtml(item.tokenPath || "General")}${item.field ? ` | ${this.escapeHtml(item.field)}` : ""}</small>
              <div>${this.escapeHtml(item.message)}</div>
            </div>`).join("")}
        </div>
      </div>
    `;
  }

  renderTokenDetail(token) {
    return `
      <div class="detail-grid">
        ${this.renderMeta("Path", token.path)}
        ${this.renderMeta("Type", token.type)}
        ${this.renderMeta("Source type", token.sourceType)}
        ${this.renderMeta("Source name", token.sourceName || "")}
        ${this.renderMeta("Source priority", String(token.sourcePriority || ""))}
        ${this.renderPreMeta("Raw value", token.rawValue)}
        ${this.renderPreMeta("Normalised value", token.normalizedValue)}
        ${this.renderPreMeta("Resolved value", token.resolvedValue)}
        ${this.renderMeta("Generated CSS variable", token.generatedCssVariable || "")}
        ${this.renderPreMeta("Generated CSS value", token.generatedCssValue)}
        ${this.renderSourceTraceMeta("Overridden sources", token.overriddenSources || [])}
        ${this.renderListMeta("Why this token exists", this.createWhyExistsLines(token))}
        ${this.renderListMeta("Resolution trace", token.resolutionTrace || [])}
        ${this.renderListMeta("Info", token.infos || [])}
        ${this.renderListMeta("Outgoing references", token.outgoingReferences || [])}
        ${this.renderListMeta("Incoming references", token.incomingReferences || [])}
        ${this.renderListMeta("Warnings", token.warnings || [])}
        ${this.renderListMeta("Errors", token.errors || [])}
      </div>
    `;
  }

  renderTokenFilters() {
    const sourceOptions = [
      ["all", "All"],
      ["Starter", "Starter"],
      ["Imported", "Imported"],
      ["CmsPrimitive", "CMS primitive"],
      ["CmsSemantic", "CMS semantic"],
      ["Component", "Component"],
    ];
    const flagOptions = [
      ["all", "All"],
      ["added", "Added"],
      ["overridden", "Overridden"],
      ["unused", "Unused"],
      ["css", "CSS generated"],
      ["tailwind-mapped", "Tailwind mapped"],
      ["tailwind-skipped", "Tailwind skipped"],
    ];

    return `
      <div class="toolbar">
        ${sourceOptions.map(([value, label]) => `<button class="${this.tokenFilters.sourceType === value ? "primary" : "subtle"}" data-token-source-filter="${this.escapeAttr(value)}">${this.escapeHtml(label)}</button>`).join("")}
      </div>
      <div class="toolbar" style="margin-top:10px;">
        ${flagOptions.map(([value, label]) => `<button class="${this.tokenFilters.flag === value ? "secondary" : "subtle"}" data-token-flag-filter="${this.escapeAttr(value)}">${this.escapeHtml(label)}</button>`).join("")}
      </div>
    `;
  }

  getFilteredTokens(tokens) {
    return (Array.isArray(tokens) ? tokens : []).filter((token) => {
      if (this.tokenFilters.sourceType !== "all" && token.sourceType !== this.tokenFilters.sourceType) {
        return false;
      }

      switch (this.tokenFilters.flag) {
        case "added": return token.isAdded;
        case "overridden": return token.isOverridden;
        case "unused": return token.isUnused;
        case "css": return token.hasGeneratedCss;
        case "tailwind-mapped": return token.isTailwindMapped;
        case "tailwind-skipped": return token.isTailwindSkipped;
        default: return true;
      }
    });
  }

  renderMergeLog(items) {
    return `
      <details ${this.mergeLogOpen ? "open" : ""}>
        <summary>Merge events (${items.length})</summary>
        <ul class="warning-list">
          ${items.map((item) => `<li>${this.escapeHtml(item.message)}</li>`).join("")}
        </ul>
      </details>
    `;
  }

  renderSourceTraceMeta(label, items) {
    return `
      <div class="detail-card">
        <strong>${this.escapeHtml(label)}</strong>
        ${Array.isArray(items) && items.length ? `<ul class="warning-list">${items.map((item) => `<li>${this.escapeHtml(`${item.sourceType}${item.sourceName ? `/${item.sourceName}` : ""} - priority ${item.sourcePriority}${item.rawValue ? ` - ${item.rawValue}` : ""}`)}</li>`).join("")}</ul>` : `<div class="empty">None</div>`}
      </div>
    `;
  }

  createWhyExistsLines(token) {
    const lines = [];
    lines.push(`Source: ${token.sourceType}${token.sourceName ? `/${token.sourceName}` : ""} - priority ${token.sourcePriority || ""}`);
    lines.push(`Type: ${token.type}`);
    lines.push(`Raw value: ${token.rawValue || "None"}`);
    if (Array.isArray(token.resolutionTrace) && token.resolutionTrace.length) {
      lines.push(`Resolved through: ${token.resolutionTrace.join(" -> ")}${token.resolvedValue ? ` -> ${token.resolvedValue}` : ""}`);
    }

    return lines;
  }

  renderMeta(label, value) {
    return `<div class="meta-card"><strong>${this.escapeHtml(label)}</strong><div>${this.escapeHtml(value || "") || '<span class="empty">None</span>'}</div></div>`;
  }

  renderPreMeta(label, value) {
    return `<div class="detail-card"><strong>${this.escapeHtml(label)}</strong><pre>${this.escapeHtml(value || "") || "None"}</pre></div>`;
  }

  renderListMeta(label, items) {
    return `
      <div class="detail-card">
        <strong>${this.escapeHtml(label)}</strong>
        ${Array.isArray(items) && items.length ? `<ul class="warning-list">${items.map((item) => `<li>${this.escapeHtml(item)}</li>`).join("")}</ul>` : `<div class="empty">None</div>`}
      </div>
    `;
  }

  groupDiagnostics(items) {
    return (Array.isArray(items) ? items : []).reduce((acc, item) => {
      const stage = item.stage || "Unknown";
      acc[stage] = acc[stage] || [];
      acc[stage].push(item);
      return acc;
    }, {});
  }

  countDiagnostics(items) {
    return Array.isArray(items) ? items.length : 0;
  }

  formatDate(value) {
    if (!value) {
      return "";
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
  }

  createDateStamp() {
    const now = new Date();
    const pad = (value) => String(value).padStart(2, "0");
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  }

  downloadText(fileName, text, mimeType) {
    const blob = new Blob([text], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  async requestJson(url, options = {}) {
    const method = String(options.method || "GET").toLowerCase();
    const headers = { ...(options.headers || {}) };
    if (options.body && !headers["Content-Type"]) {
      headers["Content-Type"] = "application/json";
    }

    const clientMethod = typeof umbHttpClient[method] === "function" ? umbHttpClient[method].bind(umbHttpClient) : null;
    if (!clientMethod) {
      throw new Error(`Unsupported request method: ${method}`);
    }

    const response = await clientMethod({
      url,
      headers,
      body: options.body,
      security: [{ scheme: "bearer", type: "http" }],
    });

    if (response.error) {
      throw new Error(this.describeError(response.error?.body || response.error, "Request failed."));
    }

    return response.data;
  }

  async requestText(url) {
    const response = await umbHttpClient.get({
      url,
      headers: { Accept: "application/json,text/plain,*/*" },
      security: [{ scheme: "bearer", type: "http" }],
    });

    if (response.error) {
      throw new Error(this.describeError(response.error?.body || response.error, "Request failed."));
    }

    return typeof response.data === "string" ? response.data : String(response.data || "");
  }

  describeError(error, fallback) {
    if (error instanceof Error && error.message) {
      return error.message;
    }

    try {
      return JSON.stringify(error);
    } catch {
      return fallback;
    }
  }

  extractNumericValue(value) {
    const match = String(value || "").match(/-?\d+(?:\.\d+)?/);
    return match ? Number(match[0]) : 0;
  }

  isHexColor(value) {
    return /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i.test(String(value || "").trim());
  }

  isColorLike(value) {
    const text = String(value || "").trim();
    return this.isHexColor(text) || /^rgba?\(/i.test(text) || /^hsla?\(/i.test(text);
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

customElements.define("site-design-token-manager", SiteDesignTokenManager);
