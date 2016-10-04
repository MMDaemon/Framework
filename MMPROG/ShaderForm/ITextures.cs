﻿using System;
using System.Collections.Generic;

namespace ShaderForm
{
	public interface ITextures : IEnumerable<string>
	{
		event EventHandler<EventArgs> OnChange;

		bool AddUpdate(string fileName);
		void Clear();
		void Remove(string fileName);
	}
}