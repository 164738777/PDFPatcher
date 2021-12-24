using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MuPdfSharp
{
	public sealed class PageLabelCollection : ICollection<PageLabel>
	{
		List<PageLabel> _labels = new List<PageLabel> ();
	
		public PageLabel this [int index] {
			get {
				return _labels[index];
			}
		}
	
		internal PageLabelCollection (MuDocument document) {
			var pl = new List<PageLabel> ();
			var l = document.Trailer.Locate ("Root/PageLabels/Nums").AsArray ();
			if (l.Kind == MuPdfObjectKind.PDF_ARRAY) {
				for (int i = 0; i < l.Count; i++) {
					var n = l[i].IntegerValue;
					var d = l[++i].AsDictionary ();
					var sp = d["St"].IntegerValue;
					var p = d["P"].StringValue;
					var s = d["S"].NameValue;
					pl.Add (new PageLabel (n, sp, p, s.Length == 0 ? PageLabelStyle.Digit : (PageLabelStyle)(byte)s[0]));
				}
				pl.Sort ();
			}
			_labels = pl;
		}
	
		/// <summary>
		/// ���ҳ���ǩ���缯���д�����ͬҳ���ҳ���ǩ�����Ƚ��ɵı�ǩɾ����������µ�ҳ���ǩ��
		/// </summary>
		/// <param name="label">��Ҫ��ӵ�ҳ���ǩ��</param>
		public void Add (PageLabel label) {
			Remove (label);
			_labels.Add (label);
			_labels.Sort ();
		}
	
		/// <summary>
		/// ���ݴ����ҳ�룬���ص�ǰҳ���ǩ���ϸ�ʽ�������ɵ�ҳ�롣
		/// </summary>
		/// <param name="pageNumber">����ҳ�롣</param>
		/// <returns>��ʽ�����ҳ���ı���</returns>
		public string Format (int pageNumber) {
			var l = _labels.Count;
			if (l == 0) {
				return String.Empty;
			}
			for (int i = l - 1; i >= 0; i--) {
				var p = _labels[i];
				if (pageNumber > p.FromPageNumber) {
					return p.Format (pageNumber);
				}
			}
			return String.Empty;
		}

		public PageLabel Find (int pageNumber) {
			--pageNumber;
			for (int i = _labels.Count - 1; i >= 0; i--) {
				if (_labels[i].FromPageNumber == pageNumber) {
					return _labels[i];
				}
			}
			return PageLabel.Empty;
		}

		public void Clear () {
			_labels.Clear ();
		}
	
		/// <summary>
		/// ���ؼ������Ƿ���������� <paramref name="item"/> ��ͬ��ʼҳ���ҳ���ǩ��
		/// </summary>
		/// <param name="item">��Ҫ�����ʼҳ���ҳ���ǩ��</param>
		/// <returns>�������ͬҳ���ҳ���ǩ������ true�����򷵻� false��</returns>
		public bool Contains (PageLabel item) {
			for (int i = _labels.Count - 1; i >= 0; i--) {
				if (_labels[i].FromPageNumber == item.FromPageNumber) {
					return true;
				}
			}
			return false;
		}
	
		public void CopyTo (PageLabel[] array, int arrayIndex) {
			_labels.CopyTo (array, arrayIndex);
		}
	
		public int Count {
			get { return _labels.Count; }
		}
	
		public bool IsReadOnly {
			get { return false; }
		}
	
		/// <summary>
		/// ɾ�������о����� <paramref name="item"/> ��ͬ��ʼҳ���ҳ���ǩ��
		/// </summary>
		/// <param name="item">��Ҫɾ����ҳ���ǩ��</param>
		/// <returns>�������ͬҳ���ҳ���ǩ������ true�����򷵻� false��</returns>
		public bool Remove (PageLabel item) {
			for (int i = _labels.Count - 1; i >= 0; i--) {
				if (_labels[i].FromPageNumber == item.FromPageNumber) {
					_labels.RemoveAt (i);
					return true;
				}
			}
			return false;
		}
	
		public IEnumerator<PageLabel> GetEnumerator () {
			return _labels.GetEnumerator ();
		}
	
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
			return _labels.GetEnumerator ();
		}
	}
}
