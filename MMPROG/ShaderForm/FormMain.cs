﻿using ControlClassLibrary;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ShaderForm
{
	public partial class FormMain : Form
	{
		private DemoModel demo;
		private bool mouseDown = false;
		private Point mousePos;
		//private int painting = 0;
		private Timing timing = new Timing(0.5f);
		const string fileNameDefaultSave = "default_save";
		const string fileExtDefaultSave = ".config.xml";
		private MultiGraph multiGraph = new MultiGraph();
		private FacadeFormMessages log = new FacadeFormMessages();
        private FacadeCamera camera = new FacadeCamera();

        public FormMain()
		{
			InitializeComponent();
			string demoFilter = fileExtDefaultSave + " (*" + fileExtDefaultSave + ")|*" + fileExtDefaultSave;
			menuSizeSetting.SelectedIndexChanged += (sender, e) => glControl.Invalidate();
			multiGraph.OnChangePosition += (pos) => soundPlayerBar1.Position = pos;
			multiGraph.OnKeyDown += FormMain_KeyDown;
			soundPlayerBar1.OnPositionChanged += (position) => glControl.Invalidate();
			soundPlayerBar1.OnPositionChanged += (position) => multiGraph.UpdatePosition(position);
			soundPlayerBar1.OnPositionChanged += (position) => camera.UpdateFromUniforms(demo.Uniforms, position);

			this.menuHelp.Click += (sender, e) => Dialogs.Help();
			this.menuLoad.Click += (sender, e) => Dialogs.OpenFile(demoFilter
				, (fileName) => LoadDemo(fileName));
			this.menuSound.Click += (sender, e) => Dialogs.OpenFile("sound (*.*)|*.*", (fileName) => demo.TimeSource.Load(fileName));
			this.MenuShaderAdd.Click += (sender, e) => Dialogs.OpenFile("glsl (*.glsl)|*.glsl", (fileName) => AddShader(fileName));
			this.MenuTextureAdd.Click += (sender, e) => Dialogs.OpenFile("texture (*.*)|*.*", (fileName) => demo.Textures.AddUpdate(fileName));
			this.menuSave.Click += (sender, e) => Dialogs.SaveFile(demoFilter, (fileName) => 
				{
					try
					{
						DemoLoader.SaveToFile(demo, fileName);
					}
					catch (Exception ex)
					{
						log.Append(ex.Message);
					}
				});
			this.menuScreenshot.Click += (sender, e) => Dialogs.SaveFile("png (*.png)|*.png", (fileName) => { glControl.Invalidate(); demo.SaveBuffer(fileName); });

			this.KeyDown += (sender, e) => { camera.KeyChange(e.KeyCode, true); glControl.Invalidate(); };
			this.KeyUp += (sender, e) => { camera.KeyChange(e.KeyCode, false); glControl.Invalidate(); };
		}

		private void AddShader(string fileName)
		{
			demo.Shaders.AddUpdateShader(fileName);
			demo.ShaderKeyframes.AddUpdate(soundPlayerBar1.Length - 0.1f, fileName);
		}

		private void LoadDemo(string fileName)
		{
			try
			{
				log.Clear();
				camera.Reset();
				DemoLoader.LoadFromFile(demo, fileName);
			}
			catch { };
		}

		private void GlControl_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Link;
		}

		private void GlControl_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files)
			{
				//check if texture
				if (!demo.Textures.AddUpdate(file))
				{
					//check if sound file
					if (!demo.TimeSource.Load(file))
					{
						//try shader
						AddShader(file);
					}
				}
			}
		}

		private void GlControl_Paint(object sender, PaintEventArgs e)
		{
			//todo: paint in other thread?
			//if (1 == painting) return;
			//System.Threading.Interlocked.Exchange(ref painting, 1);

			//normalized mouse pos
			float mouseX = this.mousePos.X / (float)glControl.Width;
			float mouseY = (glControl.Height - this.mousePos.Y) / (float)glControl.Height;
			float factor = 1.0f;
			try
			{
				factor = float.Parse(menuSizeSetting.Text.Substring(1), CultureInfo.InvariantCulture);
			}
			catch (Exception) { };

			int width = glControl.Width;
			int height = glControl.Height;

			if ('x' == menuSizeSetting.Text[0])
			{
				width = (int)Math.Round(glControl.Width * factor);
				height = (int)Math.Round(glControl.Height * factor);
			}
			else
			{
				width = (int)factor;
				height = (int)factor;
			}
			//todo: update not in main thread -> make async;
			//glControl.Context.MakeCurrent(null);
			//await Task.Run(() =>
			//{
			//	glControl.MakeCurrent();
			camera.Update(mouseX * width, mouseY * height, mouseDown);
			try
			{
				demo.UpdateBuffer((int)Math.Round(mouseX * width), (int)Math.Round(mouseY * height), mouseDown, width, height);
			}
			catch { /* We do not care about errors at this level */ }
			//	glControl.Context.MakeCurrent(null);
			//});
			//glControl.MakeCurrent();
			demo.Draw(glControl.Width, glControl.Height);
			glControl.SwapBuffers();
			timing.NewFrame();
			menuFps.Text = String.Format("{0:0.00}FPS", timing.FPS);
			//System.Threading.Interlocked.Exchange(ref painting, 0);
			if (camera.IsActive)
			{
				glControl.Invalidate();
			}
		}

		private void GlControl_Load(object sender, EventArgs eArgs)
		{
			try
			{
				demo = DemoModelFactory.Create(this);
				demo.OnSetCustomUniforms += Demo_OnSetCustomUniforms;
				demo.TimeSource.OnLoaded += Demo_OnTimeSourceLoaded;
				demo.Uniforms.OnAdd += Uniforms_OnAdd;
				demo.Uniforms.OnRemove += Uniforms_OnRemove;
				demo.Uniforms.OnAdd += multiGraph.Uniforms_OnAdd;
				demo.Uniforms.OnRemove += multiGraph.Uniforms_OnRemove;
				demo.Uniforms.OnChangeKeyframes += multiGraph.Uniforms_OnChange;
				demo.Uniforms.OnChangeKeyframes += (s, a) => camera.UpdateFromUniforms(demo.Uniforms, demo.TimeSource.Position);
				demo.Uniforms.OnChangeKeyframes += (s, a) => glControl.Invalidate();
				demo.Shaders.OnChange += Shaders_OnChange;
				demo.ShaderKeyframes.OnChange += ShaderKeframes_OnChange;
				demo.Textures.OnChange += Textures_OnChange;
			}
			catch (Exception e)
			{
				log.Append(e.Message);
			}
		}

		private void Demo_OnSetCustomUniforms(ISetUniform visualContext)
		{
			if (demo.TimeSource.IsRunning)
			{
				camera.UpdateFromUniforms(demo.Uniforms, demo.TimeSource.Position);
			}
			else
			{
				camera.SetUniforms(visualContext);
			}
		}

		private void Demo_OnTimeSourceLoaded(object sender, EventArgs e)
		{
			soundPlayerBar1.TimeSource = demo.TimeSource;
			var version = typeof(FormMain).Assembly.GetName().Version;
			Text = Path.GetFileNameWithoutExtension(demo.TimeSource.SoundFileName) + " ShaderForm: " + version.ToString();
		}

		private void Shaders_OnChange(object sender, string message)
		{
			//recreate shader menus
			while (menuShaders.DropDownItems.Count > 1) menuShaders.DropDownItems.RemoveAt(1);
			foreach (var shaderPath in demo.Shaders)
			{
				var menu = new ToolStripMenuItem();
				menu.Text = shaderPath;
				menu.ToolTipText = "Right click removes shader";
				menu.MouseDown += MenuShader_MouseDown;
				menuShaders.DropDownItems.Add(menu);
			}

			//todo: if errors disappear we would like to clear the log....
			log.Append(message);
			glControl.Invalidate();
		}

		private void ShaderKeframes_OnChange(object sender, EventArgs e)
		{
			//recreate sequence bar
			sequenceBar1.Clear();
			foreach (var shaderRatio in demo.ShaderKeyframes.CalculateRatios(soundPlayerBar1.Length))
			{
				//shader sequence
				sequenceBar1.AddItem(new SequenceBarItem(
					Path.GetFileNameWithoutExtension(shaderRatio.Item2), shaderRatio.Item2, shaderRatio.Item1));
			}
			sequenceBar1.CorrectSizes();
		}

		private void Textures_OnChange(object sender, EventArgs e)
		{
			//recreate texture menus
			while (menuTextures.DropDownItems.Count > 1) menuTextures.DropDownItems.RemoveAt(1);
			foreach (var textureName in demo.Textures)
			{
				var menu = new ToolStripMenuItem();
				menu.Text = textureName;
				menu.ToolTipText = "Right click removes shader";
				menu.MouseDown += MenuTexture_MouseDown;
				menuTextures.DropDownItems.Add(menu);
			}
			glControl.Invalidate();
		}

		private void Uniforms_OnAdd(object sender, string uniformName)
		{
			var menu = new ToolStripMenuItem();
			menu.Name = uniformName;
			menu.Text = uniformName;
			menu.ToolTipText = "Right click removes uniform";
			menu.MouseDown += MenuUniform_MouseDown;
			menuUniforms.DropDownItems.Add(menu);
			var menuCopy = new ToolStripMenuItem();
			menuCopy.Text = "Copy";
			menuCopy.ToolTipText = "copy keyframe data to clipboard";
			menuCopy.Click += (snder, args) => demo.Uniforms.GetKeyFrames(uniformName).CopyKeyframesToClipboard();
			menu.DropDownItems.Add(menuCopy);
			var menuPaste = new ToolStripMenuItem();
			menuPaste.Text = "Paste";
			menuPaste.ToolTipText = "paste keyframe data from clipboard";
			menuPaste.Click += (snder, args) => demo.Uniforms.GetKeyFrames(uniformName).PasteKeyframesFromClipboard();
			menu.DropDownItems.Add(menuPaste);
		}

		private void Uniforms_OnRemove(object sender, string uniformName)
		{
			menuUniforms.DropDownItems.RemoveByKey(uniformName);
		}

		private void FormMain_Load(object sender, EventArgs e)
		{
			try
			{
				Microsoft.Win32.RegistryKey keyApp = Application.UserAppDataRegistry;
				if (null == keyApp) return;
				this.LoadLayout();

				string granularity = Convert.ToString(keyApp.GetValue("granularity", this.menuSizeSetting.Text));
				this.menuSizeSetting.SelectedIndex = this.menuSizeSetting.FindString(granularity);

				String[] arguments = Environment.GetCommandLineArgs();
				if (arguments.Length > 1)
				{
					AddShader(arguments[1]);
				}
				else
				{
					LoadDemo(fileNameDefaultSave + fileExtDefaultSave);
				}
				soundPlayerBar1.Playing = Convert.ToBoolean(keyApp.GetValue("play", false));
				soundPlayerBar1.Position = (float)Convert.ToDouble(keyApp.GetValue("time", 0.0));
			}
			catch (Exception ex)
			{
				log.Append(ex.Message);
			}
		}

		private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			try
			{
				Microsoft.Win32.RegistryKey keyApp = Application.UserAppDataRegistry;
				if (null == keyApp)
				{
					return;
				}
				this.SaveLayout();
				multiGraph.SaveLayout();
				log.SaveLayout();
				keyApp.SetValue("play", soundPlayerBar1.Playing);
				keyApp.SetValue("granularity", this.menuSizeSetting.Text);
				keyApp.SetValue("time", this.soundPlayerBar1.Position);
				//rename old
				try
				{
					var dt = File.GetLastWriteTime(fileNameDefaultSave + fileExtDefaultSave);
					File.Move(fileNameDefaultSave + fileExtDefaultSave, fileNameDefaultSave + " " + DateTime.Now.ToString("yyyyMMdd HHmmss") + fileExtDefaultSave);
				}
				catch { }
				DemoLoader.SaveToFile(demo, fileNameDefaultSave + fileExtDefaultSave);
				demo.Dispose();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void FormMain_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Handled) return;
            switch (e.KeyCode)
			{
				case Keys.Escape: Close(); return;
				case Keys.C: camera.AddKeyFrames(soundPlayerBar1.Position, demo.Uniforms); break;
				case Keys.K:
					if (e.Control)
					{
						multiGraph.AddInterpolatedKeyframeTo(sender, soundPlayerBar1.Position);
					}
					else
					{
						multiGraph.AddInterpolatedKeyframeToVisible(soundPlayerBar1.Position);
					}
					break;
				case Keys.Space:
					soundPlayerBar1.Playing = !soundPlayerBar1.Playing;
					e.Handled = true;
					break;
				case Keys.Left: soundPlayerBar1.Position -= e.Alt ? 0.1f : 0.5f; break;
				case Keys.Right: soundPlayerBar1.Position += e.Alt ? 0.1f : 0.5f; break;
				case Keys.PageDown: soundPlayerBar1.Position += e.Alt ? 2.0f : 5.0f; break;
				case Keys.PageUp: soundPlayerBar1.Position -= e.Alt ? 2.0f : 5.0f; break;
				case Keys.Home: soundPlayerBar1.Position = 0.0f; break;
				case Keys.End: soundPlayerBar1.Position = soundPlayerBar1.Length; break;
			}
		}

        private void GlControl_MouseDown(object sender, MouseEventArgs e)
		{
			if (MouseButtons.Left == e.Button)
			{
				mouseDown = true;
				glControl.Invalidate();
			}
		}

		private void GlControl_MouseMove(object sender, MouseEventArgs e)
		{
			mousePos = e.Location;
			if(!soundPlayerBar1.Playing) glControl.Invalidate(); //otherwise time stops during update?!
		}

		private void GlControl_MouseUp(object sender, MouseEventArgs e)
		{
			if (MouseButtons.Left == e.Button)
			{
				mouseDown = false;
				glControl.Invalidate();
			}
		}

		private void Reload_Click(object sender, EventArgs e)
		{
			soundPlayerBar1.Position = 0.0f;
			//todo: reload
		}

		private void MenuTexture_MouseDown(object sender, MouseEventArgs e)
		{
			if (MouseButtons.Right != e.Button) return;
			var menu = sender as ToolStripMenuItem;
			if (null == menu) return;
			demo.Textures.Remove(menu.Text);
		}

		private void MenuShader_MouseDown(object sender, MouseEventArgs e)
		{
			var menu = sender as ToolStripMenuItem;
			if (null == menu) return;
			switch (e.Button)
			{
				case MouseButtons.Left:
					demo.ShaderKeyframes.AddUpdate(soundPlayerBar1.Length - 0.1f, menu.Text);
					break;
				case MouseButtons.Right:
					demo.Shaders.RemoveShader(menu.Text);
					demo.ShaderKeyframes.RemoveAllWithName(menu.Text);
					break;
			}
		}

		private void MenuUniform_MouseDown(object sender, MouseEventArgs e)
		{
			var menu = sender as ToolStripMenuItem;
			if (null == menu) return;
			switch (e.Button)
			{
				case MouseButtons.Left: ShowUniformGraph(menu.Text); break;
				case MouseButtons.Right: demo.Uniforms.Remove(menu.Text); break;
			}
		}

		private void MenuSound_MouseDown(object sender, MouseEventArgs e)
		{
			if (MouseButtons.Right != e.Button) return;
			demo.TimeSource.Load(string.Empty);
		}

		private void ShowUniformGraph(string uniformName)
		{
			multiGraph.Show(uniformName);
		}

		private void MenuFullscreen_CheckedChanged(object sender, EventArgs e)
		{
			if (menuFullscreen.Checked)
			{
				//ordering important
				this.TopMost = true;
				this.FormBorderStyle = FormBorderStyle.None;
				this.WindowState = FormWindowState.Maximized;
				this.menuStrip.Visible = false;
				this.panelSequence.Visible = false;
				this.soundPlayerBar1.Visible = false;
			}
			else
			{
				this.TopMost = false;
				this.FormBorderStyle = FormBorderStyle.Sizable;
				this.WindowState = FormWindowState.Normal;
				this.menuStrip.Visible = true;
				this.panelSequence.Visible = true;
				this.soundPlayerBar1.Visible = true;
			}
		}

		private void sequenceBar1_OnChanged(object sender, EventArgs e)
		{
			//no udpate events during loading
			demo.ShaderKeyframes.OnChange -= ShaderKeframes_OnChange;
			demo.ShaderKeyframes.Clear();
			var ratios = sequenceBar1.Items.Select((item) => new Tuple<float, string>(item.Ratio, item.Data as string));
			var keyframes = ShaderKeyframes.CalculatePosFromRatios(ratios, soundPlayerBar1.Length);
			foreach (var kf in keyframes)
			{
				demo.ShaderKeyframes.AddUpdate(kf.Item1, kf.Item2);
			}
			demo.ShaderKeyframes.OnChange += ShaderKeframes_OnChange;
			glControl.Invalidate();
		}

		private void TextUniformAdd_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keys.Enter == e.KeyCode)
			{
				string text = TextUniformAdd.Text;
				if (demo.Uniforms.Add(text))
				{
					var kfs = demo.Uniforms.GetKeyFrames(text);
					TextUniformAdd.Text = string.Empty;
					if (null == kfs) return;
					kfs.AddUpdate(0.0f, 0.0f);
					kfs.AddUpdate(soundPlayerBar1.Length, soundPlayerBar1.Length);
					ShowUniformGraph(text);
				}
			}
		}

		private void TextUniformAdd_TextChanged(object sender, EventArgs e)
		{
			TextUniformAdd.BackColor = UniformHelper.IsNameValid(TextUniformAdd.Text) ? Color.PaleGreen : Color.LightSalmon;
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			demo.Clear();
			camera.Reset();
		}

		private void addCameraUniformsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			camera.AddKeyFrames(demo.TimeSource.Position, demo.Uniforms);
		}
	}
}