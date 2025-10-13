using System.Collections.Generic;
using Helion.Geometry.Vectors;

namespace Helion.Window.Input;

public interface ITouchAdapter
{
    void Poll();

    IReadOnlyList<Vec2F> GetTouch();
}