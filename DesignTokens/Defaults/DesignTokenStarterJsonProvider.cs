namespace Site.DesignTokens.Defaults;

public sealed class DesignTokenStarterJsonProvider : IDesignTokenStarterJsonProvider
{
    public string GetStarterJson() =>
        """
        {
          "color": {
            "brand": {
              "primary": {
                "$type": "color",
                "$value": "#0055ff"
              },
              "secondary": {
                "$type": "color",
                "$value": "#111827"
              }
            },
            "text": {
              "default": {
                "$type": "color",
                "$value": "#111827"
              },
              "muted": {
                "$type": "color",
                "$value": "#5c6a7d"
              }
            },
            "surface": {
              "page": {
                "$type": "color",
                "$value": "#ffffff"
              },
              "card": {
                "$type": "color",
                "$value": "#ffffff"
              },
              "input": {
                "$type": "color",
                "$value": "#ffffff"
              }
            }
          },
          "semantic": {
            "action": {
              "primary": {
                "background": {
                  "$type": "color",
                  "$value": "{color.brand.primary}"
                },
                "text": {
                  "$type": "color",
                  "$value": "#ffffff"
                }
              },
              "secondary": {
                "background": {
                  "$type": "color",
                  "$value": "{color.surface.page}"
                },
                "text": {
                  "$type": "color",
                  "$value": "{color.brand.primary}"
                },
                "border": {
                  "$type": "color",
                  "$value": "{color.brand.primary}"
                }
              }
            },
            "surface": {
              "page": {
                "background": {
                  "$type": "color",
                  "$value": "{color.surface.page}"
                }
              },
              "card": {
                "background": {
                  "$type": "color",
                  "$value": "{color.surface.card}"
                }
              },
              "input": {
                "background": {
                  "$type": "color",
                  "$value": "{color.surface.input}"
                }
              }
            },
            "text": {
              "default": {
                "$type": "color",
                "$value": "{color.text.default}"
              },
              "muted": {
                "$type": "color",
                "$value": "{color.text.muted}"
              }
            },
            "link": {
              "default": {
                "$type": "color",
                "$value": "{color.brand.primary}"
              }
            },
            "border": {
              "default": {
                "$type": "color",
                "$value": "{color.brand.primary}"
              },
              "strong": {
                "$type": "color",
                "$value": "{color.brand.secondary}"
              }
            },
            "input": {
              "text": {
                "$type": "color",
                "$value": "{color.text.default}"
              },
              "border": {
                "$type": "color",
                "$value": "{color.brand.secondary}"
              },
              "focusRing": {
                "$type": "color",
                "$value": "{color.brand.primary}"
              }
            }
          },
          "space": {
            "sm": {
              "$type": "dimension",
              "$value": {
                "mobile": {
                  "value": 8,
                  "unit": "px"
                },
                "tablet": {
                  "value": 12,
                  "unit": "px"
                },
                "desktop": {
                  "value": 16,
                  "unit": "px"
                }
              }
            },
            "md": {
              "$type": "dimension",
              "$value": {
                "mobile": {
                  "value": 16,
                  "unit": "px"
                },
                "tablet": {
                  "value": 24,
                  "unit": "px"
                },
                "desktop": {
                  "value": 32,
                  "unit": "px"
                }
              }
            },
            "lg": {
              "$type": "dimension",
              "$value": {
                "mobile": {
                  "value": 24,
                  "unit": "px"
                },
                "tablet": {
                  "value": 32,
                  "unit": "px"
                },
                "desktop": {
                  "value": 40,
                  "unit": "px"
                }
              }
            }
          },
          "radius": {
            "sm": {
              "$type": "dimension",
              "$value": {
                "value": 8,
                "unit": "px"
              }
            },
            "md": {
              "$type": "dimension",
              "$value": {
                "value": 12,
                "unit": "px"
              }
            },
            "lg": {
              "$type": "dimension",
              "$value": {
                "value": 18,
                "unit": "px"
              }
            }
          },
          "font": {
            "family": {
              "sans": {
                "$type": "fontFamily",
                "$value": "Inter, Arial, sans-serif"
              }
            },
            "weight": {
              "regular": {
                "$type": "fontWeight",
                "$value": 400
              },
              "bold": {
                "$type": "fontWeight",
                "$value": 700
              }
            }
          },
          "typography": {
            "body": {
              "$type": "typography",
              "$value": {
                "fontFamily": "{font.family.sans}",
                "fontWeight": "{font.weight.regular}",
                "fontSize": {
                  "value": 16,
                  "unit": "px"
                },
                "lineHeight": 1.5
              }
            },
            "heading": {
              "$type": "typography",
              "$value": {
                "fontFamily": "{font.family.sans}",
                "fontWeight": "{font.weight.bold}",
                "fontSize": {
                  "value": 32,
                  "unit": "px"
                },
                "lineHeight": 1.15
              }
            }
          },
          "shadow": {
            "card": {
              "$type": "shadow",
              "$value": {
                "color": "#00000022",
                "offsetX": {
                  "value": 0,
                  "unit": "px"
                },
                "offsetY": {
                  "value": 4,
                  "unit": "px"
                },
                "blur": {
                  "value": 12,
                  "unit": "px"
                },
                "spread": {
                  "value": 0,
                  "unit": "px"
                }
              }
            },
            "panel": {
              "$type": "shadow",
              "$value": {
                "color": "#0f172a14",
                "offsetX": {
                  "value": 0,
                  "unit": "px"
                },
                "offsetY": {
                  "value": 10,
                  "unit": "px"
                },
                "blur": {
                  "value": 24,
                  "unit": "px"
                },
                "spread": {
                  "value": -8,
                  "unit": "px"
                }
              }
            }
          },
          "border": {
            "default": {
              "$type": "border",
              "$value": {
                "width": {
                  "value": 1,
                  "unit": "px"
                },
                "style": "solid",
                "color": "{color.brand.primary}"
              }
            }
          },
          "motion": {
            "fast": {
              "$type": "duration",
              "$value": {
                "value": 150,
                "unit": "ms"
              }
            }
          },
          "opacity": {
            "disabled": {
              "$type": "number",
              "$value": 0.5
            }
          },
          "component": {
            "button": {
              "primary": {
                "background": {
                  "$type": "color",
                  "$value": "{semantic.action.primary.background}"
                },
                "text": {
                  "$type": "color",
                  "$value": "{semantic.action.primary.text}"
                },
                "border": {
                  "$type": "color",
                  "$value": "{semantic.action.primary.background}"
                },
                "radius": {
                  "$type": "dimension",
                  "$value": "{radius.md}"
                }
              },
              "secondary": {
                "background": {
                  "$type": "color",
                  "$value": "{semantic.action.secondary.background}"
                },
                "text": {
                  "$type": "color",
                  "$value": "{semantic.action.secondary.text}"
                },
                "border": {
                  "$type": "color",
                  "$value": "{semantic.action.secondary.border}"
                },
                "radius": {
                  "$type": "dimension",
                  "$value": "{radius.md}"
                }
              }
            },
            "card": {
              "background": {
                "$type": "color",
                "$value": "{semantic.surface.card.background}"
              },
              "radius": {
                "$type": "dimension",
                "$value": "{radius.lg}"
              },
              "shadow": {
                "$type": "shadow",
                "$value": "{shadow.card}"
              },
              "border": {
                "$type": "color",
                "$value": "{semantic.border.default}"
              }
            },
            "input": {
              "background": {
                "$type": "color",
                "$value": "{semantic.surface.input.background}"
              },
              "text": {
                "$type": "color",
                "$value": "{semantic.input.text}"
              },
              "border": {
                "$type": "color",
                "$value": "{semantic.input.border}"
              },
              "focusRing": {
                "$type": "color",
                "$value": "{semantic.input.focusRing}"
              },
              "radius": {
                "$type": "dimension",
                "$value": "{radius.md}"
              }
            }
          },
          "enabledThemeVariants": ["default"],
          "themes": {
            "dark": {
              "$name": "Dark",
              "$selector": "[data-theme=\"dark\"]",
              "color": {
                "brand": {
                  "primary": {
                    "$type": "color",
                    "$value": "#66a3ff"
                  },
                  "secondary": {
                    "$type": "color",
                    "$value": "#cbd5e1"
                  }
                },
                "text": {
                  "default": {
                    "$type": "color",
                    "$value": "#f8fafc"
                  }
                },
                "surface": {
                  "page": {
                    "$type": "color",
                    "$value": "#0f172a"
                  },
                  "card": {
                    "$type": "color",
                    "$value": "#111c33"
                  },
                  "input": {
                    "$type": "color",
                    "$value": "#13203a"
                  }
                }
              },
              "semantic": {
                "text": {
                  "muted": {
                    "$type": "color",
                    "$value": "#cbd5e1"
                  }
                },
                "action": {
                  "secondary": {
                    "background": {
                      "$type": "color",
                      "$value": "#13203a"
                    },
                    "text": {
                      "$type": "color",
                      "$value": "#f8fafc"
                    },
                    "border": {
                      "$type": "color",
                      "$value": "#66a3ff"
                    }
                  }
                }
              }
            },
            "highContrast": {
              "$name": "High Contrast",
              "$selector": "[data-theme=\"high-contrast\"]",
              "color": {
                "brand": {
                  "primary": {
                    "$type": "color",
                    "$value": "#ffff00"
                  }
                },
                "text": {
                  "default": {
                    "$type": "color",
                    "$value": "#ffffff"
                  }
                },
                "surface": {
                  "page": {
                    "$type": "color",
                    "$value": "#000000"
                  },
                  "card": {
                    "$type": "color",
                    "$value": "#000000"
                  },
                  "input": {
                    "$type": "color",
                    "$value": "#000000"
                  }
                }
              },
              "semantic": {
                "border": {
                  "default": {
                    "$type": "color",
                    "$value": "#ffffff"
                  },
                  "strong": {
                    "$type": "color",
                    "$value": "#ffff00"
                  }
                },
                "input": {
                  "focusRing": {
                    "$type": "color",
                    "$value": "#ffff00"
                  }
                }
              }
            },
            "ocean": {
              "$name": "Ocean",
              "$type": "Brand",
              "$selector": "[data-theme=\"ocean\"]",
              "color": {
                "brand": {
                  "primary": {
                    "$type": "color",
                    "$value": "#0077b6"
                  },
                  "secondary": {
                    "$type": "color",
                    "$value": "#00b4d8"
                  }
                }
              }
            }
          }
        }
        """;
}
