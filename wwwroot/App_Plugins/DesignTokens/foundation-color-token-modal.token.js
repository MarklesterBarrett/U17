import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export const SITE_FOUNDATION_COLOR_TOKEN_MODAL_ALIAS = "Site.Modal.FoundationColorToken";

export const SITE_FOUNDATION_COLOR_TOKEN_MODAL = new UmbModalToken(
  SITE_FOUNDATION_COLOR_TOKEN_MODAL_ALIAS,
  {
    modal: {
      type: "sidebar",
      size: "small",
    },
  },
);
