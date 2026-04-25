using MTS.Models;

namespace MTS.Utils;

public interface ICustomEquals<T>
{
    bool CustomEquals(T other);
}