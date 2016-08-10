﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ShaderForm
{
	public class MultiGraph
	{
		public delegate void OnChangePositionHandler(float position);
		public event OnChangePositionHandler OnChangePosition;
		public event KeyEventHandler OnKeyDown;

		public void AddInterpolatedKeyframeTo(object sender, float position)
		{
			foreach (var graph in graphs.Values)
			{
				if (graph.IsYourForm(sender))
				{
					graph.AddInterpolatedKeyframe(position);
					return;
				}
			}
		}

		public void AddInterpolatedKeyframeToVisible(float position)
		{
			foreach (var graph in graphs.Values)
			{
				if (!graph.IsVisible) continue;
				graph.AddInterpolatedKeyframe(position);
			}
		}

		public void SaveLayout()
		{
			foreach (var graph in graphs.Values)
			{
				graph.SaveData();
			}
		}

		public void Show(string uniformName)
		{
			if (!graphs.ContainsKey(uniformName)) return;
			graphs[uniformName].Show();
		}

		public void Uniforms_OnRemove(object sender, string uniformName)
		{
			if (string.IsNullOrEmpty(uniformName)) return;
			if (graphs.ContainsKey(uniformName))
			{
				graphs[uniformName].Close();
				graphs.Remove(uniformName);
			}
		}

		public void Uniforms_OnAdd(object sender, string uniformName)
		{
			if (string.IsNullOrEmpty(uniformName)) return;
			var uniforms = sender as IUniforms;
			if (null == uniforms) return;
			var kfs = uniforms.GetKeyFrames(uniformName);
			var visualisation = new FacadeKeyframesVisualisation(uniformName, kfs);
			visualisation.OnChangePosition += (position) => { if (null != OnChangePosition) OnChangePosition(position); };
			visualisation.OnKeyDown += (s, a) => { if (null != OnKeyDown) OnKeyDown(s, a); };
			graphs.Add(uniformName, visualisation);
		}

		public void Uniforms_OnChange(object sender, string uniformName)
		{
			if (string.IsNullOrEmpty(uniformName)) return;
			if (graphs.ContainsKey(uniformName))
			{
				graphs[uniformName].Update();
			}
		}

		public void UpdatePosition(float position)
		{
			foreach (var graph in graphs.Values)
			{
				graph.UpdatePosition(position);
			}
		}

		private Dictionary<string, FacadeKeyframesVisualisation> graphs = new Dictionary<string, FacadeKeyframesVisualisation>();
	}
}
