$root = "C:\Users\D\Documents\VisualStudio\2025\Mars\tests"
$enc = [System.Text.Encoding]::GetEncoding(28591)

$map = [ordered]@{
    'DeleteUser_TryDeleteSingleAdmin_ValidationsErrorOfDisallowDeleteSingleAdmin' = 'DeleteUser_TryDeleteSingleAdmin_ReturnsValidationError'
    'Inject_MissingRequiredArg_FailsResult' = 'Inject_MissingRequiredArg_ReturnsFailResult'
    'CompletedTasks_HaveCount_CreatesTaskAndReturnCount' = 'CompletedTasks_HaveCount_CreatesTaskAndReturnsCount'
    'CurrentTasks_HaveCount_CreatesTaskAndReturnCount' = 'CurrentTasks_HaveCount_CreatesTaskAndReturnsCount'
    'CreatePostJson_MetaValueAgainstValidator_RejectsInvalidAndAcceptValid' = 'CreatePostJson_MetaValueAgainstValidator_RejectsInvalidAndAcceptsValid'
    'MoveFiles_ToAnotherFolder_MovesFileAndUpdateFolderId' = 'MoveFiles_ToAnotherFolder_MovesFileAndUpdatesFolderId'
    'UpdatePostTypePresentation_WithGridSettings_StoresAndReturnThem' = 'UpdatePostTypePresentation_WithGridSettings_StoresAndReturnsThem'
    'TryMatch_StaticUrlShouldBePrioritizedOverPatternUrl_MatchesStatic' = 'TryMatch_StaticAndPatternUrlBothMatch_StaticWins'
    'ListFeedbackPagination_Request_ShouldValidTotalCount' = 'ListFeedbackPagination_Request_ReturnsValidTotalCount'
    'DeleteFolder_FolderWithFiles_ShouldError466' = 'DeleteFolder_FolderWithFiles_FailsWithError466'
    'Upload_FileNotAllowedExtensions_ShouldValidateError' = 'Upload_FileNotAllowedExtensions_ReturnsValidationError'
    'Upload_FileTooLarge_ShouldValidateError' = 'Upload_FileTooLarge_ReturnsValidationError'
    'UploadPlugin_OnDisallowUploadZipManually_Should466Denied' = 'UploadPlugin_OnDisallowUploadZipManually_FailsWith466Denied'
    'CreatePostCategory_CreateChildElement_ShouldValidPath' = 'CreatePostCategory_CreateChildElement_BuildsValidPath'
    'UpdatePostCategory_UpdateParentSlug_ShouldChildElementRecalcPath' = 'UpdatePostCategory_UpdateParentSlug_RecalcsChildPaths'
    'DeletePostCategoryType_TryDeleteDefaultPostCategoryType_ShouldValidationError' = 'DeletePostCategoryType_TryDeleteDefaultPostCategoryType_ReturnsValidationError'
    'DeleteUserType_TryDeleteDefaultUserType_ShouldValidationError' = 'DeleteUserType_TryDeleteDefaultUserType_ReturnsValidationError'
    'UploadFile_DisallowMultipartNode_ShouldUnsupportedStatusError' = 'UploadFile_DisallowMultipartNode_ReturnsUnsupportedStatusError'
    'Execute_ChangeDictionaryField_ShouldSuccessReturnCorrentDictionaryType' = 'Execute_ChangeDictionaryField_SucceedsAndReturnsCorrectDictionaryType'
    'ExecuteForCountAggregation_ValidCount_ShouldOnePack' = 'ExecuteForCountAggregation_ValidCount_EmitsSinglePack'
    'ExecuteForInputAggregation_NotAllInputsReceived_ShouldNotNextReturn' = 'ExecuteForInputAggregation_NotAllInputsReceived_DoesNotReturnNext'
    'ExecuteForInputAggregation_WhenTimeoutExpiresNotAllInputsReceived_ShouldTimeoutAndReturnAggregated' = 'ExecuteForInputAggregation_TimeoutWithMissingInputs_TimesOutAndReturnsAggregated'
    'Execute_TerminalteJobsShouldWorkCorrectly_SuccessWithoutException' = 'Execute_TerminateJobs_SucceedsWithoutException'
    'Execute_SetResultToProperty_ShouldDidntChangePayload' = 'Execute_SetResultToProperty_DoesNotChangePayload'
    'Execute_SetExtraField_FieldShouldWriteInNodeMsg' = 'Execute_SetExtraField_WritesFieldToNodeMsg'
    'Execute_SetExtraFieldByPropertyPath_FieldShouldWriteInNodeMsg' = 'Execute_SetExtraFieldByPropertyPath_WritesFieldToNodeMsg'
    'Generate_ShouldCleanForbiddenCharacters_ToPreventPathTraversal' = 'Generate_ForbiddenCharacters_CleansThemToPreventPathTraversal'
    'Generate_ShouldReplaceFileTokensCorrectly_WhenValidTemplateProvided' = 'Generate_ValidTemplateWithFileTokens_ReplacesFileTokensCorrectly'
    'Generate_ShouldThrowArgumentNullException_WhenTemplateIsEmpty' = 'Generate_EmptyTemplate_ThrowsArgumentNullException'
    'AreConnected_ShouldReturnFalse_WhenNoPath' = 'AreConnected_NoPath_ReturnsFalse'
    'AreConnected_ShouldReturnTrue_WhenPathExists' = 'AreConnected_PathExists_ReturnsTrue'
    'AreDirectlyConnected_ShouldReturnFalse_WhenNotConnected' = 'AreDirectlyConnected_NotConnected_ReturnsFalse'
    'AreDirectlyConnected_ShouldReturnTrue_WhenConnected' = 'AreDirectlyConnected_Connected_ReturnsTrue'
    'FileAbsolutePath_PassPath_ShouldExpectResult' = 'FileAbsolutePath_VariousPaths_ReturnsExpected'
    'FileAbsoluteUrlFromPath_PassPath_ShouldExpectResult' = 'FileAbsoluteUrlFromPath_VariousPaths_ReturnsExpected'
    'FileRelativeUrlFromPath_PassPath_ShouldExpectResult' = 'FileRelativeUrlFromPath_VariousPaths_ReturnsExpected'
    'GetExtension_PassPath_ShouldExpectExt' = 'GetExtension_VariousPaths_ReturnsExpectedExtension'
    'IsImage_PassPath_ShouldExpectResult' = 'IsImage_VariousPaths_ReturnsExpected'
    'NormalizePathSlash_PassPath_ShouldExpectResult' = 'NormalizePathSlash_VariousPaths_ReturnsExpected'
    'EqBlock_DictionaryCaseInsensevity_ShouldWorkExpect' = 'EqBlock_DictionaryCaseInsensitivity_Works'
    'EqBlock_ExpandoObjectCaseInsensevity_ShouldWorkExpect' = 'EqBlock_ExpandoObjectCaseInsensitivity_Works'
    'OutputVariable_CaseInsensetive_ShouldWorkExpect' = 'OutputVariable_CaseInsensitive_Works'
    'GetFeedback_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetFeedback_NotExistEntity_Fails404ReturnsNull'
    'GetFile_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetFile_NotExistEntity_Fails404ReturnsNull'
    'GetNavMenu_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetNavMenu_NotExistEntity_Fails404ReturnsNull'
    'GetOption_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetOption_NotExistEntity_Fails404ReturnsNull'
    'GetPostJson_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetPostJson_NotExistEntity_Fails404ReturnsNull'
    'GetPost_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetPost_NotExistEntity_Fails404ReturnsNull'
    'GetPostType_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetPostType_NotExistEntity_Fails404ReturnsNull'
    'GetRole_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetRole_NotExistEntity_Fails404ReturnsNull'
    'GetUser_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetUser_NotExistEntity_Fails404ReturnsNull'
    'GetUserType_NotExistEntity_Fail404ShouldReturnNullInsteadException' = 'GetUserType_NotExistEntity_Fails404ReturnsNull'
    'EditorJsContent_ModifiedTimestamp_ShouldToDateTime' = 'EditorJsContent_ModifiedTimestamp_ConvertsToDateTime'
}

$total = 0
$files = Get-ChildItem $root -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch 'obj|bin' }
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $text = $enc.GetString($bytes)
    $orig = $text
    $count = 0
    foreach ($k in $map.Keys) {
        if ($text.Contains($k)) {
            $idx = 0
            while (($idx = $text.IndexOf($k, $idx)) -ge 0) { $count++; $idx += $k.Length }
            $text = $text.Replace($k, $map[$k])
        }
    }
    if ($text -ne $orig) {
        [System.IO.File]::WriteAllBytes($f.FullName, $enc.GetBytes($text))
        Write-Output (($f.FullName.Substring($root.Length + 1)) + ' : ' + $count)
        $total += $count
    }
}
Write-Output ('TOTAL replacements: ' + $total)
