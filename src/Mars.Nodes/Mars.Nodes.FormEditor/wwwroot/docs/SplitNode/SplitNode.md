# SplitNode

This node is designed to decompose (separate) complex or composite data into individual elements. It accepts a single input message and generates a series of output messages, invoking the next processing step for each received part.

## How it works

The node automatically detects the type of incoming data and applies the appropriate splitting strategy:

1. **Strings**:
- Splits text into substrings using the specified **Delimiter**.
- If the delimiter field is left **empty**, the text will be split character by character (each letter or symbol will become a separate message).
2. **Arrays and Lists** (IEnumerable):
- Iterates over each element of the collection and sends it to the output separately.
3. **Complex Objects**:
- Parses the object and creates a separate message for each of its public properties. The output is a structure containing the property name (`PropertyName`) and its value (`Value`).
