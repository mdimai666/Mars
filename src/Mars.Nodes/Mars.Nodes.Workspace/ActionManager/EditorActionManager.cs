using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mars.Core.Exceptions;
using Mars.Nodes.EditorApi.Interfaces;
using Microsoft.Extensions.Logging;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace.ActionManager;

public class EditorActionManager : IEditorActionManager, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly List<EditorActionType> _actions = [];
    private readonly INodeEditorApi _nodeEditor;
    private readonly HotKeysContext _hotkeysContext;

    HashSet<Assembly> assemblies = [];
    object _lock = new { };
    bool invalide = true;
    ILogger _logger;

    private readonly Stack<IEditorHistoryAction> _undoStack = new();
    private readonly Stack<IEditorHistoryAction> _redoStack = new();
    private const int MaxHistory = 30;

    public EditorActionManager(INodeEditorApi nodeEditorApi, HotKeysContext hotkeysContext)
    {
        _nodeEditor = nodeEditorApi;
        _hotkeysContext = hotkeysContext;
        _logger = _nodeEditor.CreateLogger<EditorActionManager>();
    }

    public void RegisterAction(Type actionType)
    {
        var attr = actionType.GetCustomAttribute<EditorActionCommandAttribute>()
                        ?? throw new ArgumentException("Action must have [EditorActionHotkeyAttribute]");
        var at = new EditorActionType(actionType, attr);
        _actions.Add(at);

        if (at.ActiveHotkey is not null)
        {
            var k = at.ActiveHotkey.Value;
            _hotkeysContext.Add(k.Modifiers, k.Code, () => ExecuteAction(actionType), at.ActionType.Name);
        }
        _logger.LogTrace($"RegisterAction '{actionType}', hotkey='{at.ActiveHotkey}'");
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
    }

    public void RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;

        lock (_lock)
        {
            _actions.Clear();

            foreach (var assembly in assemblies)
            {
                var _dict = GetActionImplements(assembly);

                foreach (var a in _dict)
                {
                    var attr = a.GetCustomAttribute<EditorActionCommandAttribute>();
                    if (attr is null) continue;
                    RegisterAction(a);
                }
            }
        }
    }

    public static IEnumerable<Type> GetActionImplements(Assembly assembly)
    {
        var type = typeof(IEditorAction);

        var types = assembly.GetTypes()
                                .Where(p =>
                                    type.IsAssignableFrom(p)
                                    && p.IsPublic
                                    && p.IsClass
                                    && !p.IsAbstract
                                );

        return types;
    }

    /// <summary>
    /// Получить все действия с хоткеями
    /// </summary>
    public IEnumerable<Type> GetAllActionTypes() => _actions.Select(s => s.ActionType);

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
    {
        ExecuteAction(typeof(TAction));
    }

    public void ExecuteAction(Type actionType)
    {
        _logger.LogTrace($"ExecuteAction('{actionType}')");
        var a = _actions.FirstOrDefault(s => s.ActionType == actionType)
                        ?? throw new NotFoundException($"action '{actionType}' not found");

        var instance = (IEditorAction)Activator.CreateInstance(a.ActionType, _nodeEditor)!;

        if (instance.CanExecute())
        {
            instance.Execute();

            if (instance is IEditorHistoryAction historyAction)
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

    public void ExecuteAction(IEditorAction actionInstance)
    {
        _logger.LogTrace($"ExecuteAction((instance)'{actionInstance.GetType()}')");

        if (actionInstance.CanExecute())
        {
            actionInstance.Execute();

            if (actionInstance is IEditorHistoryAction historyAction)
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

}

static class StackExtensions
{
    public static void TrimExcessHistory<T>(this Stack<T> stack, int maxCount)
    {
        while (stack.Count > maxCount)
            stack.Reverse().Skip(1); // или переложить в новый стек
    }
}
