using System;
using System.Collections.Generic;
using System.Text;

namespace GenericSol.Games;

/// <summary>
/// Non-generic marker used wherever the concrete options type isn't known statically
/// (e.g. <see cref="IGame.GetOptions"/>/<see cref="IGame.SetOptions"/>).
/// </summary>
public interface IJsonSerializable
{
    /// <summary>
    /// Return a JSON representation of this object
    /// </summary>
    /// <returns>A JSON string representing this object</returns>
    String ToJson();
}

/// <summary>
/// Implemented by a game's concrete options type to allow deserializing a JSON string
/// back into an instance without needing an existing instance to call into (see
/// <see cref="TSelf.FromJson"/>).
/// </summary>
/// <typeparam name="TSelf">The concrete options type implementing this interface</typeparam>
public interface IJsonSerializable<TSelf> : IJsonSerializable where TSelf : IJsonSerializable<TSelf>
{
    /// <summary>
    /// Deserialize a JSON string into a new instance of <typeparamref name="TSelf"/>
    /// </summary>
    /// <param name="json">A JSON string previously produced by <see cref="IJsonSerializable.ToJson"/></param>
    /// <returns>A new deserialized instance</returns>
    static abstract TSelf FromJson(string json);
}
