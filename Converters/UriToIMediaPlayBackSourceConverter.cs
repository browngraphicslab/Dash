﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;

namespace Dash.Converters
{
	/// <summary>
	///   Converts Uri to IMediaPlayBackSource, which the is the source data for MediaPlayerElement
	/// </summary>
	class UriToIMediaPlayBackSourceConverter : SafeDataToXamlConverter<Uri, IMediaPlaybackSource>
	{
		private UriToIMediaPlayBackSourceConverter() { }

		public static UriToIMediaPlayBackSourceConverter Instance;

		static UriToIMediaPlayBackSourceConverter()
		{
			Instance = new UriToIMediaPlayBackSourceConverter();
		}

		public override IMediaPlaybackSource ConvertDataToXaml(Uri data, object parameter = null)
		{
			return MediaSource.CreateFromUri(data);
		}

		public override Uri ConvertXamlToData(IMediaPlaybackSource xaml, object parameter = null)
		{
			throw new NotImplementedException("no way to convert from imediaplaybacksource to uri");
		}
	}
}
