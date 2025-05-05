using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Core.Interfaces;

public interface ICanCopy<T> where T : class
{
    public T Copy(T instance);
}
