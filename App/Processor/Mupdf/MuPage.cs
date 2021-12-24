using System;
using System.Drawing;

namespace MuPdfSharp
{
	public sealed class MuPage : IDisposable
	{
		#region ���й���Դ��Ա
		private ContextHandle _context;
		private DocumentHandle _document;
		private PageHandle _page;
		private DisplayListHandle _displayList;
		#endregion

		#region �й���Դ��Ա
		static readonly ImageRendererOptions __defaultOptions = new ImageRendererOptions ();
		MuCookie _cookie;
		MuTextPage _TextPage;
		bool _flattened;

		/// <summary>��ȡ��ǰҳ���ҳ�롣</summary>
		public int PageNumber { get; private set; }
		/// <summary>��ȡ��ǰҳ��ĳߴ磨���½�������Ϊ��0,0�����������ȡҳ���ֵ��е�ԭʼ����������ʹ�� <see cref="VisualBound"/> ���ԡ�</summary>
		public Rectangle Bound {
			get {
				return NativeMethods.BoundPage (_context, _page);
			}
		}
		/// <summary>��ȡ��ǰҳ�������������꼰�ߴ硣</summary>
		public Rectangle VisualBound => Matrix.Identity.RotateTo(Rotation).Transform(VisualBox);

		public Rectangle ArtBox => LookupPageBox("ArtBox");
		public Rectangle BleedBox => LookupPageBox("BleedBox");
		public Rectangle CropBox => LookupPageBox("CropBox");
		public Rectangle TrimBox => LookupPageBox("TrimBox");
		public Rectangle MediaBox => LookupPageBox("MediaBox");
		public Rectangle VisualBox { get { var b = LookupPageBox("CropBox"); return b.IsEmpty ? LookupPageBox("MediaBox") : b; } }
		public int Rotation => LookupPage("Rotate").IntegerValue;

		public MuTextPage TextPage {
			get {
				PopulateTextPage();
				return _TextPage;
			}
		}

		private unsafe Rectangle LookupPageBox (string name) {
			if (_flattened == false) {
				var d = _page.PageDictionary;
				NativeMethods.FlatternInheritablePageItems(_context, d);
				_flattened = true;
			}
			var a = new MuPdfDictionary (_context, _page.PageDictionary);
			var ra = a[name].AsArray();
			return ra.Count == 4 ? Rectangle.FromArray (a[name]) : Rectangle.Empty;
		}
		private MuPdfObject LookupPage (string name) {
			var a = new MuPdfDictionary (_context, _page.PageDictionary);
			return a[name];
		}
		#endregion

		internal MuPage (ContextHandle context, DocumentHandle document, int pageNumber, ref MuCookie cookie) {
			try {
				_page = new PageHandle (document, pageNumber - 1);
				_document = document;
				_context = context;
				_cookie = cookie;
				PageNumber = pageNumber;
			}
			catch (AccessViolationException) {
				_page.DisposeHandle ();
				throw new MuPdfException ("�޷����ص� " + pageNumber + " ҳ��");
			}
		}

		///// <summary>
		///// ��ȡָ��������ı���
		///// </summary>
		///// <param name="selection">����</param>
		///// <returns>�����ڵ��ı���</returns>
		//public string GetSelection (Rectangle selection) {
		//    return Interop.DecodeUtf8String (NativeMethods.CopySelection (_context, GetTextPage (), selection));
		//}

		///// <summary>
		///// ��ȡָ��������ı���
		///// </summary>
		///// <param name="selection">����</param>
		///// <returns>�����ڵ��ı���</returns>
		//public List<Rectangle> HighlightSelection (Rectangle selection) {
		//	var l = 
		//	return Interop.DecodeUtf8String (NativeMethods.HighlightSelection (_context, _page, selection));
		//}

		/// <summary>
		/// ʹ��Ĭ�ϵ�������Ⱦҳ�档
		/// </summary>
		/// <param name="width">ҳ��Ŀ�ȡ�</param>
		/// <param name="height">ҳ��ĸ߶ȡ�</param>
		/// <returns>��Ⱦ�����ɵ� <see cref="Bitmap"/>��</returns>
		public FreeImageAPI.FreeImageBitmap RenderPage (int width, int height) {
			return RenderPage (width, height, __defaultOptions);
		}

		/// <summary>
		/// ʹ��ָ����������Ⱦҳ�档
		/// </summary>
		/// <param name="width">ҳ��Ŀ�ȡ�</param>
		/// <param name="height">ҳ��ĸ߶ȡ�</param>
		/// <param name="options">��Ⱦѡ�</param>
		/// <returns>��Ⱦ�����ɵ� <see cref="FreeImageAPI.FreeImageBitmap"/>��</returns>
		public FreeImageAPI.FreeImageBitmap RenderPage (int width, int height, ImageRendererOptions options) {
			using (var pix = InternalRenderPage (width, height, options)) {
				if (pix != null) {
					return pix.ToFreeImageBitmap (options);
				}
			}
			return null;
		}

		/// <summary>
		/// ʹ��ָ����������Ⱦҳ�档
		/// </summary>
		/// <param name="width">ҳ��Ŀ�ȡ�</param>
		/// <param name="height">ҳ��ĸ߶ȡ�</param>
		/// <param name="options">��Ⱦѡ�</param>
		/// <returns>��Ⱦ�����ɵ� <see cref="Bitmap"/>��</returns>
		public Bitmap RenderBitmapPage (int width, int height, ImageRendererOptions options) {
			using (var pix = InternalRenderPage (width, height, options)) {
				if (pix != null) {
					return pix.ToBitmap (options);
				}
			}
			return null;
		}

		public MuFont GetFont(MuTextChar character) {
			return new MuFont(_context, character.FontID);
		}
		public MuFont GetFont(MuTextSpan span) {
			return new MuFont(_context, span.FontID);
		}
		private DisplayListHandle GetDisplayList () {
			if (_displayList.IsValid ()) {
				return _displayList;
			}
			_displayList = _context.CreateDisplayList (Bound);
			using (var d = new DeviceHandle(_context, _displayList)) {
				//if (hideAnnotations) {
				//	NativeMethods.RunPageContents (_document, _page, d, ref m, _cookie);
				//}
				//else {
					NativeMethods.RunPage (_context, _page, d, Matrix.Identity, ref _cookie);
				d.EndOperations();
				//}
			}
			if (_cookie.ErrorCount > 0) {
				System.Diagnostics.Debug.WriteLine("�ڵ� " + PageNumber + " ҳ�� " + _cookie.ErrorCount + " ������");
			}
			return _displayList;
		}

		void PopulateTextPage () {
			if (_TextPage != null) {
				return;
			}
			var vb = VisualBound;
			var text = new TextPageHandle (_context, vb);
			try {
				using (var dev = new DeviceHandle (_context, text)) {
					NativeMethods.RunDisplayList (_context, GetDisplayList(), dev, Matrix.Identity, vb, ref _cookie);
					dev.EndOperations();
				}
				_TextPage = new MuTextPage(text);
			}
			catch (AccessViolationException) {
				text.DisposeHandle ();
				throw;
			}
			return;
		}

		private PixmapData InternalRenderPage (int width, int height, ImageRendererOptions options) {
			var b = this.Bound;
			if (b.Width == 0 || b.Height == 0) {
				return null;
			}
			var ctm = CalculateMatrix (width, height, options);
			var bbox = width > 0 && height > 0 ? new BBox (0, 0, width, height) : ctm.Transform (b).Round;

			var pix = _context.CreatePixmap (options.ColorSpace, bbox);
			try {
				NativeMethods.ClearPixmap (_context, pix, 0xFF);
				using (var dev = new DeviceHandle (_context, pix, Matrix.Identity)) {
					if (options.LowQuality) {
						NativeMethods.EnableDeviceHints (_context, dev, DeviceHints.IgnoreShade | DeviceHints.DontInterporateImages | DeviceHints.NoCache);
					}
					if (_cookie.IsCancellationPending) {
						return null;
					}
					NativeMethods.RunPageContents (_context, _page, dev, ctm, ref _cookie);
					if (options.HideAnnotations == false) {
						NativeMethods.RunPageAnnotations(_context, _page, dev, ctm, ref _cookie);
						NativeMethods.RunPageWidgets(_context, _page, dev, ctm, ref _cookie);
					}
					//NativeMethods.BeginPage (dev, ref b, ref ctm);
					//NativeMethods.RunDisplayList (_context, GetDisplayList(), dev, ctm, ctm.Transform(VisualBound), ref _cookie);
					//NativeMethods.EndPage (dev);

					dev.EndOperations();

					if (_cookie.IsCancellationPending) {
						return null;
					}
					var pd = new PixmapData (_context, pix);
					if (options.TintColor != Color.Transparent) {
						pd.Tint (options.TintColor);
					}
					if (options.Gamma != 1.0f) {
						pd.Gamma (options.Gamma);
					}
					return pd;
				}
			}
			catch (AccessViolationException) {
				pix.DisposeHandle ();
				throw new MuPdfException ("�޷���Ⱦҳ�棺" + PageNumber);
			}
		}

		private Matrix CalculateMatrix (int width, int height, ImageRendererOptions options) {
			float w = width, h = height;
			var b = Bound;
			if (options.UseSpecificWidth) {
				if (w < 0) {
					w = -w;
				}
				if (h < 0) {
					h = -h;
				}
				if (options.FitArea && w != 0 && h != 0) {
					var rw = w / b.Width;
					var rh = h / b.Height;
					if (rw < rh) {
						h = 0;
					}
					else {
						w = 0;
					}
				}
				if (w == 0 && h == 0) {	// No resize
					w = b.Width;
					h = b.Height;
				}
				else if (h == 0) {
					h = (float)width * b.Height / b.Width;
				}
				else if (w == 0) {
					w = (float)height * b.Width / b.Height;
				}
			}
			else if (w == 0 || h == 0) {
				w = b.Width * options.ScaleRatio * options.Dpi / 72;
				h = b.Height * options.ScaleRatio * options.Dpi / 72;
			}

			var ctm = Matrix.Scale (w / b.Width, h / b.Height).RotateTo (options.Rotation);
			if (options.VerticalFlipImages) {
				ctm = Matrix.Concat (ctm, Matrix.VeritcalFlip);
			}
			if (options.HorizontalFlipImages) {
				ctm = Matrix.Concat (ctm, Matrix.HorizontalFlip);
			}
			return ctm;
		}

		/// <summary>
		/// ��ȡҳ�����ݵ�ʵ�ʸ��Ƿ�Χ��
		/// </summary>
		/// <returns>����ҳ�����ݵ���С <see cref="BBox"/>��</returns>
		public Rectangle GetContentBoundary () {
			var b = Bound;
			var o = b;
			using (var dev = new DeviceHandle (_context, ref o)) {
				try {
					var im = Matrix.Identity;
					//NativeMethods.BeginPage (dev, ref b, ref im);
					NativeMethods.RunDisplayList (_context, GetDisplayList (), dev, Matrix.Identity, b, ref _cookie);
					dev.EndOperations();
					//NativeMethods.EndPage (dev);
					return o;
				}
				catch (AccessViolationException) {
					throw new MuPdfException ("�޷���ȡҳ�����ݱ߿�" + PageNumber);
				}
			}
		}

		#region ʵ�� IDisposable �ӿڵ����Ժͷ���
		private bool disposed;
		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);	// ������������
		}

		/// <summary>�ͷ��� MuPdfPage ռ�õ���Դ��</summary>
		/// <param name="disposing">�Ƿ��ֶ��ͷ��й���Դ��</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_page")]
		void Dispose (bool disposing) {
			if (!disposed) {
				if (disposing) {
					#region �ͷ��й���Դ
					if (_TextPage != null) {
						_TextPage.Dispose();
					}
					_TextPage = null;
					#endregion
				}

				#region �ͷŷ��й���Դ
				// ע�����ﲻ���̰߳�ȫ��
				//int retry = 0;
				//_cookie.CancelAsync ();
				//while (_cookie.IsRunning && ++retry < 10) {
				//    System.Threading.Thread.Sleep (100);
				//}
				_page.DisposeHandle ();
				_displayList.DisposeHandle ();
				_document = null;
				#endregion
			}
			disposed = true;
		}

		// ��������ֻ��δ���� Dispose ����ʱ����
		// �������в������ṩ��������
		~MuPage () {
			Dispose (false);
		}
		#endregion

		//protected override bool ReleaseHandle () {
		//	NativeMethods.FreePage (_document, this.handle);
		//	return true;
		//}
	}
}
