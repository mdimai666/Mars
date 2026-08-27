using Microsoft.JSInterop;
using System;

namespace Mars.Admin.Framework.Models {
    public class MessageUpdateInvokeHelper
    {
        private Action action;

        public MessageUpdateInvokeHelper(Action action)
        {
            this.action = action;
        }

        [JSInvokable("AppFront")]
        public void UpdateMessageCaller()
        {
            action.Invoke();
        }
    }
}