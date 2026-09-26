using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mars.Admin.Framework;
using Mars.Core.Exceptions;
using Mars.Nodes.Front.Abstractions.Editor.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace.ActionManager;

public class EditorActionManager : IEditorActionManager, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyDictionary<Type, EditorActionType> Actions => _actions;

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly INodeEditorApi _nodeEditor;
    private readonly IServiceProvider _serviceProvider;
    private readonly HotKeysContext _hotkeysContext;
    private readonly EditorActionLocator _editorActionLocator;
    private readonly AdminJs _adminJs;
    private ILogger _logger;
    private IReadOnlyDictionary<Type, EditorActionType> _actions;

    private readonly Stack<IEditorHistoryAction> _undoStack = new();
    private readonly Stack<IEditorHistoryAction> _redoStack = new();
    private const int MaxHistory = 30;

    private ICopyBufferItem? _copyBuffer;

    public EditorActionManager(INodeEditorApi nodeEditorApi,
                                IServiceProvider serviceProvider,
                                HotKeysContext hotkeysContext,
                                EditorActionLocator editorActionLocator,
                                AdminJs adminJs)
    {
        _nodeEditor = nodeEditorApi;
        _serviceProvider = serviceProvider;
        _hotkeysContext = hotkeysContext;
        _editorActionLocator = editorActionLocator;
        _adminJs = adminJs;
        _logger = _nodeEditor.CreateLogger<EditorActionManager>();
        _actions = _editorActionLocator.Actions.ToDictionary(s => s.ActionType);
        BuildActions();
    }

    private void BuildActions()
    {
        foreach (var action in _actions.Values)
        {
            RegisterAction(action);
        }
    }

    private void RegisterAction(EditorActionType action)
    {
        if (action.ActiveHotkey is not null)
        {
            var k = action.ActiveHotkey.Value;
            _hotkeysContext.Add(k.Modifiers, k.Code, () => ExecuteAction(action.ActionType), action.ActionType.Name);
        }
        _logger.LogTrace("RegisterAction '{ActionType}', hotkey='{Hotkey}'", action.ActionType, action.ActiveHotkey);
    }

    /// <summary>
    /// Получить все действия с хоткеями
    /// </summary>
    public IEnumerable<Type> GetAllActionTypes() => _actions.Values.Select(s => s.ActionType);

    /// <summary>
    /// Позволяет пользователю переназначить хоткей
    /// </summary>
    public void SetUserHotkey(IEditorAction action, string newHotkey)
    {
        //var ah = _actions.FirstOrDefault(a => a.Action == action);
        //if (ah == null) return;

        //// Удаляем старый привязанный хоткей
        //_hotkeysContext.Remove(ah.ActiveHotkey);

        //// Сохраняем новый
        //ah.UserHotkey = newHotkey;

        //// Добавляем новый в HotKeys2
        //_hotkeysContext.Add(ah.ActiveHotkey, () =>
        //{
        //    if (action.CanExecute())
        //        action.Execute();
        //});
    }

    public void ExecuteAction<TAction>() where TAction : IEditorAction
        => ExecuteAction(typeof(TAction));

    public void ExecuteAction<TAction>(bool addToHistory) where TAction : IEditorAction
        => ExecuteAction(typeof(TAction), addToHistory);

    public void ExecuteAction(Type actionType, bool addToHistory = true)
    {
        _logger.LogTrace($"ExecuteAction('{actionType}')");
        var a = _actions.GetValueOrDefault(actionType)
                        ?? throw new NotFoundException($"action '{actionType}' not found");

        var instance = HasNodeEditorApiConstructor(a.ActionType)
            ? (IEditorAction)ActivatorUtilities.CreateInstance(_serviceProvider, a.ActionType, [_nodeEditor])!
            : (IEditorAction)ActivatorUtilities.CreateInstance(_serviceProvider, a.ActionType)!;

        if (instance.CanExecute())
        {
            instance.Execute();

            if (addToHistory && instance is IEditorHistoryAction historyAction)
            {
                _undoStack.Push(historyAction);
                if (_undoStack.Count > MaxHistory)
                    _undoStack.TrimExcessHistory(MaxHistory);

                ClearRedoStack();

                Notify(nameof(CanUndo));
                Notify(nameof(CanRedo));
            }
        }
    }

    readonly Dictionary<Type, bool> _nodeEditorApiCtorCache = [];

    bool HasNodeEditorApiConstructor(Type type)
    {
        if (_nodeEditorApiCtorCache.TryGetValue(type, out var cached)) return cached;

        return _nodeEditorApiCtorCache[type] = type
            .GetConstructors()
            .Any(ctor => ctor
                .GetParameters()
                .Any(p => p.ParameterType == typeof(INodeEditorApi)));
    }

    public void ExecuteAction(IEditorAction actionInstance, bool addToHistory = true)
    {
        _logger.LogTrace($"ExecuteAction((instance)'{actionInstance.GetType()}')");

        if (actionInstance.CanExecute())
        {
            actionInstance.Execute();

            if (addToHistory && actionInstance is IEditorHistoryAction historyAction)
            {
                _undoStack.Push(historyAction);
                if (_undoStack.Count > MaxHistory)
                    _undoStack.TrimExcessHistory(MaxHistory);

                ClearRedoStack();

                Notify(nameof(CanUndo));
                Notify(nameof(CanRedo));
            }
        }
    }

    void ClearRedoStack()
    {
        foreach (var a in _redoStack)
            if (a is IDisposable disposable) disposable.Dispose();
        _redoStack.Clear();
    }

    public void ReplaceLastAction(IEditorHistoryAction actionInstance)
    {
        if (_undoStack.TryPop(out var last) && last is IDisposable disposable)
            disposable.Dispose();
        _undoStack.Push(actionInstance);
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        _logger.LogTrace("Undo()");

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        Notify(nameof(CanUndo));
        Notify(nameof(CanRedo));
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        _logger.LogTrace("Redo()");

        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);

        Notify(nameof(CanUndo));
        Notify(nameof(CanRedo));
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void SetCopyBuffer(ICopyBufferItem copyBufferItem)
    {
        if (_copyBuffer is IDisposable disposable) disposable.Dispose();
        _copyBuffer = copyBufferItem;
    }
    public bool IsHaveCopyBuffer => _copyBuffer != null;
    public void PasteCopiedBuffer()
    {
        if (_copyBuffer?.CanPaste() ?? false)
        {
            _copyBuffer.Paste();
        }
    }

    public ValueTask CopyToClipboard(string text)
    {
        return _adminJs.CopyToClipboard(text);
    }
}

static class StackExtensions
{
    public static void TrimExcessHistory<T>(this Stack<T> stack, int maxCount)
    {
        if (stack.Count <= maxCount) return;

        // Stack перечисляется сверху: Take — свежие (оставить), Skip — старые (выбросить)
        foreach (var dropped in stack.Skip(maxCount))
            if (dropped is IDisposable disposable) disposable.Dispose();

        var kept = stack.Take(maxCount).ToArray();
        stack.Clear();
        for (var i = kept.Length - 1; i >= 0; i--)
            stack.Push(kept[i]);
    }
}
