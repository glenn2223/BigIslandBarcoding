﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace ZXing.Net.Maui
{

	public partial class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>
	{
		public static PropertyMapper<ICameraView, CameraViewHandler> CameraViewMapper = new()
		{
			[nameof(ICameraView.IsTorchOn)] = (handler, virtualView) => handler.cameraManager.UpdateTorch(virtualView.IsTorchOn),
			[nameof(ICameraView.CameraLocation)] = (handler, virtualView) => handler.cameraManager.UpdateCameraLocation(virtualView.CameraLocation)
		};

		public static CommandMapper<ICameraView, CameraViewHandler> CameraCommandMapper = new()
		{
			[nameof(ICameraView.Focus)] = MapFocus,
			[nameof(ICameraView.AutoFocus)] = MapAutoFocus,
		};

		CameraManager cameraManager;

		public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

		public CameraViewHandler() : base(CameraViewMapper)
		{
		}

		public CameraViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraViewMapper)
		{
		}


		protected override async void ConnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			base.ConnectHandler(nativeView);

			if (await cameraManager.CheckPermissions())
				cameraManager.Connect();

			cameraManager.FrameReady += CameraManager_FrameReady;
		}

		void CameraManager_FrameReady(object sender, CameraFrameBufferEventArgs e)
			=> FrameReady?.Invoke(this, e);

		protected override void DisconnectHandler(NativePlatformCameraPreviewView nativeView)
		{
			cameraManager.FrameReady -= CameraManager_FrameReady;

			cameraManager.Disconnect();

			base.DisconnectHandler(nativeView);
		}

		public void Dispose()
			=> cameraManager?.Dispose();

		public void Focus(Point point)
			=> cameraManager?.Focus(point);

		public void AutoFocus()
			=> cameraManager?.AutoFocus();

		public static void MapFocus(CameraViewHandler handler, ICameraView cameraBarcodeReaderView, object? parameter)
		{
			if (parameter is not Point point)
				throw new ArgumentException("Invalid parameter", "point");

			handler.Focus(point);
		}

		public static void MapAutoFocus(CameraViewHandler handler, ICameraView cameraBarcodeReaderView, object? parameters)
			=> handler.AutoFocus();

		protected override NativePlatformCameraPreviewView CreatePlatformView()
		{
			if (cameraManager == null)
				cameraManager = new(MauiContext, VirtualView?.CameraLocation ?? CameraLocation.Rear);
#if IOS || MACCATALYST
			var v = cameraManager.CreatePlatformView();
#elif ANDROID
			var v = cameraManager.CreateNativeView();
#endif
			return v;
		}
	}
}
