namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Готовые сценарии контекста «browser». Владелец JSON — хост (стенд):
/// страница редактора получает пример через api/PxBlocks/Samples/browser
/// и загружает его в полотно кнопкой. Формат — нативный Blockly serialization
/// (строки в сокетах — shadow-блоки «text»).
/// </summary>
public static class PxBrowserSample
{
    /// <summary>
    /// Открыть Википедию, найти «Россия» и вывести первые три результата поиска.
    /// Enter в шапке Википедии при точном совпадении ведёт прямо на статью —
    /// сценарий показывает её, затем открывает полнотекстовый список результатов
    /// (fulltext=1) и выводит первые три заголовка.
    /// </summary>
    public const string WikipediaSearchJson = """
    {
      "blocks": {
        "languageVersion": 0,
        "blocks": [
          {
            "type": "core.events.start",
            "id": "start",
            "x": 40,
            "y": 40,
            "inputs": {
              "DO": {
                "block": {
                  "type": "demostand.playwright.goto",
                  "id": "gotoWiki",
                  "inputs": {
                    "URL": {
                      "shadow": { "type": "core.text.text", "id": "urlText", "fields": { "TEXT": "https://ru.wikipedia.org" } }
                    }
                  },
                  "next": {
                    "block": {
                      "type": "demostand.playwright.type",
                      "id": "typeQuery",
                      "inputs": {
                        "TEXT": {
                          "shadow": { "type": "core.text.text", "id": "queryText", "fields": { "TEXT": "Россия" } }
                        },
                        "SELECTOR": {
                          "shadow": { "type": "core.text.text", "id": "inputSelector", "fields": { "TEXT": "#searchInput" } }
                        }
                      },
                      "next": {
                        "block": {
                          "type": "demostand.playwright.press",
                          "id": "pressEnter",
                          "fields": { "KEY": "Enter" },
                          "next": {
                            "block": {
                              "type": "demostand.playwright.wait_selector",
                              "id": "waitArticle",
                              "inputs": {
                                "SELECTOR": {
                                  "shadow": { "type": "core.text.text", "id": "articleSelector", "fields": { "TEXT": "#firstHeading" } }
                                }
                              },
                              "next": {
                                "block": {
                                  "type": "demostand.playwright.goto",
                                  "id": "gotoResults",
                                  "inputs": {
                                    "URL": {
                                      "shadow": { "type": "core.text.text", "id": "resultsUrlText", "fields": { "TEXT": "https://ru.wikipedia.org/w/index.php?search=Россия&fulltext=1" } }
                                    }
                                  },
                                  "next": {
                                    "block": {
                                      "type": "demostand.playwright.wait_selector",
                                      "id": "waitResults",
                                      "inputs": {
                                        "SELECTOR": {
                                          "shadow": { "type": "core.text.text", "id": "resultsSelector", "fields": { "TEXT": ".mw-search-results" } }
                                        }
                                      },
                                      "next": {
                                        "block": {
                                          "type": "demostand.playwright.print_texts",
                                          "id": "printResults",
                                          "fields": { "COUNT": 3 },
                                          "inputs": {
                                            "SELECTOR": {
                                              "shadow": { "type": "core.text.text", "id": "headingSelector", "fields": { "TEXT": ".mw-search-result-heading" } }
                                            }
                                          }
                                        }
                                      }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        ]
      }
    }
    """;
}
