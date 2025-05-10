# HtmlParseNode

Extracts elements from an xml/html document held in <mark>msg.payload</mark> using CSS selectors.

## Input - payload:string
the html string from which to extract elements.

## Output - payload:array | string
the result can be either a single message with a payload containing an array of the matched elements, or multiple messages that each contain a matched element.

## Details
This node supports CSS selectors. See the css-select <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_selectors" target="_blank">documentation</a> for more information on the supported syntax.

Output can be plain text, html or complex mapping to objects using sub selectors.
