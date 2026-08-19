using System;
using System.Collections.Generic;
using System.Text;

namespace GenericSol.Games;

public interface IJsonSerializable
{
    /// <summary>
    /// Return a JSON representation of this object
    /// </summary>
    /// <returns>A JSON string representing this object</returns>
    String ToJson();
    IJsonSerializable FromJson(string json);
}
