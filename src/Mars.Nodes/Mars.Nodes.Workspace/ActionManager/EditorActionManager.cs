using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mars.Core.Exceptions;
using Mars.Nodes.Front.Shared.Editor.Models;
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
    private readonly EditorActionLocator _edittorActionLocator;
    private ILogger _logger;
    private IReadOnlyDictionary<Type, EditorActionType> _actions;

    private readonly Stack<IEditorHistoryAction> _undoStack = new();
    private readonly Stack<IEditorHistoryAction> _redoStack = new();
    private const int MaxHistory = 30;

    private ICopyBufferItem? _copyBuffer;

    public EditorActionManager(INodeEditorApi nodeEditorApi,
                                IServiceProvider serviceProvider,
                                HotKeysContext hotkeysContext,
                                EditorActionLocator edittorActionLocator)
    {
        _nodeEditor = nodeEditorApi;
        _serviceProvider = serviceProvider;
        _hotkeysContext = hotkeysContext;
        _edittorActionLocator = edittorActionLocator;
        _logger = _nodeEditor.CreateLogger<EditorActionManager>();
        _actions = _edittorActionLocator.Actions.ToDictionary(s => s.ActionType);
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
        _logger.LogTrace($"RegisterAction '{_actions}', hotkey='{action.ActiveHotkey}'");
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

                _redoStack.Clear();

                Notify(nameof(CanUndo));
                Notify(nameof(CanRedo));
                //Tools.SetTimeout(_nodeEditor.CallStateHasChanged, 1);
            }
        }
    }

    bool HasNodeEditorApiConstructor(Type type)
    {
        return type
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

                _redoStack.Clear();

                Notify(nameof(CanUndo));
                Notify(nameof(CanRedo));
                //Tools.SetTimeout(_nodeEditor.CallStateHasChanged, 1);
            }
        }
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
        //Tools.SetTimeout(_nodeEditor.CallStateHasChanged, 1);
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
        //Tools.SetTimeout(_nodeEditor.CallStateHasChanged, 1);
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void SetCopyBuffer(ICopyBufferItem copyBufferItem)
        => _copyBuffer = copyBufferItem;
    public bool IsHaveCopyBuffer => _copyBuffer != null;
    public void PasteCopiedBuffer()
    {
        if (_copyBuffer?.CanPaste() ?? false)
        {
            _copyBuffer.Paste();
        }
    }
}

static class StackExtensions
{
    public static void TrimExcessHistory<T>(this Stack<T> stack, int maxCount)
    {
        while (stack.Count > maxCount)
            stack.Reverse().Skip(1); // или переложить в новый стек
    }
}
