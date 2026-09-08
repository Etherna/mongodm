// Copyright 2020-present Etherna SA
// This file is part of Scrinium.
// 
// Scrinium is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// Scrinium is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with Scrinium.
// If not, see <https://www.gnu.org/licenses/>.
using System;

namespace Etherna.Scrinium.Core.Serialization.Modifiers
{
    /// <summary>
    /// The serializer modifiers surface invoked only inside the library. Implemented
    /// explicitly by <see cref="SerializerModifierAccessor"/>.
    /// </summary>
    internal interface IInternalSerializerModifierAccessor
    {
        // Properties.
        /// <summary>
        /// True while the root documents deserialize detached from the current scope: their
        /// instances stay out of the identity map and of the change tracking, while the
        /// references they carry keep resolving through the identity map like any
        /// deserialization. The root instance is a carrier of the document state, refreshing
        /// an instance the scope already holds.
        /// </summary>
        bool IsDetachedRootEnabled { get; }

        // Methods.
        /// <summary>
        /// Deserialize the root documents detached from the current scope, on the current
        /// execution flow, until the returned scope is disposed.
        /// </summary>
        /// <returns>The modifier scope</returns>
        IDisposable EnableDetachedRootSerializerModifier();
    }
}
