using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPlaybackLib
{
    public interface IExceptionNotifier
    {
        event EventHandler<ExceptionEventArgs> ExceptionRaised;        
    }

    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception e)
        {
            this.Exception = e;
        }


        public Exception Exception
        {
            get;
            set;
        }

    }
}
