// v5 FluentTextInput renders the bound Value as the host `value` ATTRIBUTE, which for an
// <input> has defaultValue semantics: after the user types, re-rendering with Value="" does
// NOT clear the shadow control. Setting the host `value` PROPERTY does clear it (FAST syncs
// the property to the inner control reactively), so InputTags2 calls this after committing a tag.
export function clear(element) {
    if (element) {
        element.value = '';
    }
}
