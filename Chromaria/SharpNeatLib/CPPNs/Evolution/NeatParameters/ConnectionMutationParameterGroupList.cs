using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.Evolution
{
	public class ConnectionMutationParameterGroupList : List<ConnectionMutationParameterGroup>
	{
		#region Constructors

		public ConnectionMutationParameterGroupList()
		{}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		public ConnectionMutationParameterGroupList(ConnectionMutationParameterGroupList copyFrom)
		{
			foreach(ConnectionMutationParameterGroup paramGroup in copyFrom)
				Add(new ConnectionMutationParameterGroup(paramGroup));
		}

		#endregion
	}
}
