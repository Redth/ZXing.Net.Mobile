// <copyright company="APX Labs, Inc.">
//     Copyright (c) APX Labs, Inc. All rights reserved.
// </copyright>
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// Many thanks to Jonathan Pryor from Xamarin for his assistance

using System;
using Android.Hardware;
using Android.Runtime;

namespace ApxLabs.FastAndroidCamera
{
	public static class CameraExtensions
	{
		static IntPtr id_addCallbackBuffer_arrayB;
		public static void AddCallbackBuffer(this Camera self, FastJavaByteArray callbackBuffer)
		{
			if (id_addCallbackBuffer_arrayB == IntPtr.Zero)
				id_addCallbackBuffer_arrayB = JNIEnv.GetMethodID(self.Class.Handle, "addCallbackBuffer", "([B)V");
			JNIEnv.CallVoidMethod(self.Handle, id_addCallbackBuffer_arrayB, new JValue(callbackBuffer.Handle));
		}

		static IntPtr id_setPreviewCallback_Landroid_hardware_Camera_PreviewCallback_;
		public static void SetNonMarshalingPreviewCallback(this Camera self, INonMarshalingPreviewCallback cb)
		{
			if (id_setPreviewCallback_Landroid_hardware_Camera_PreviewCallback_ == IntPtr.Zero)
				id_setPreviewCallback_Landroid_hardware_Camera_PreviewCallback_ = JNIEnv.GetMethodID(self.Class.Handle, "setPreviewCallbackWithBuffer", "(Landroid/hardware/Camera$PreviewCallback;)V");
			JNIEnv.CallVoidMethod(self.Handle, id_setPreviewCallback_Landroid_hardware_Camera_PreviewCallback_, new JValue(cb));
		}

		static IntPtr id_setOneShotPreviewCallback_Landroid_hardware_Camera_PreviewCallback_;
		public static void SetNonMarshalingOneShotPreviewCallback(this Camera self, INonMarshalingPreviewCallback cb)
		{
			if (id_setOneShotPreviewCallback_Landroid_hardware_Camera_PreviewCallback_ == IntPtr.Zero)
				id_setOneShotPreviewCallback_Landroid_hardware_Camera_PreviewCallback_ = JNIEnv.GetMethodID(self.Class.Handle, "setOneShotPreviewCallback", "(Landroid/hardware/Camera$PreviewCallback;)V");
			JNIEnv.CallVoidMethod(self.Handle, id_setOneShotPreviewCallback_Landroid_hardware_Camera_PreviewCallback_, new JValue(cb));
		}
	}

	// Metadata.xml XPath interface reference: path="/api/package[@name='android.hardware']/interface[@name='Camera.PreviewCallback']"
	[Register("android/hardware/Camera$PreviewCallback", "", "ApxLabs.FastAndroidCamera.INonMarshalingPreviewCallbackInvoker")]
	public interface INonMarshalingPreviewCallback : IJavaObject {

		// Metadata.xml XPath method reference: path="/api/package[@name='android.hardware']/interface[@name='Camera.PreviewCallback']/method[@name='onPreviewFrame' and count(parameter)=2 and parameter[1][@type='byte[]'] and parameter[2][@type='android.hardware.Camera']]"
//		[Register("onPreviewFrame", "([BLandroid/hardware/Camera;)V", "GetOnPreviewFrame_arrayBLandroid_hardware_Camera_Handler:ApxLabs.FastAndroidCamera.INonMarshalingPreviewCallbackInvoker, FastAndroidCamera, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[Register("onPreviewFrame", "([BLandroid/hardware/Camera;)V", "GetOnPreviewFrame_arrayBLandroid_hardware_Camera_Handler:ApxLabs.FastAndroidCamera.INonMarshalingPreviewCallbackInvoker, ZXingNetMobile, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		void OnPreviewFrame(IntPtr data, Camera camera);
	}

	[Register("android/hardware/Camera$PreviewCallback", DoNotGenerateAcw=true)]
	internal class INonMarshalingPreviewCallbackInvoker : Java.Lang.Object, INonMarshalingPreviewCallback {

		static IntPtr java_class_ref = JNIEnv.FindClass("android/hardware/Camera$PreviewCallback");
		IntPtr class_ref;

		public static INonMarshalingPreviewCallback GetObject(IntPtr handle, JniHandleOwnership transfer)
		{
			return GetObject<INonMarshalingPreviewCallback>(handle, transfer);
		}

		static IntPtr Validate(IntPtr handle)
		{
			if (!JNIEnv.IsInstanceOf(handle, java_class_ref))
				throw new InvalidCastException(string.Format("Unable to convert instance of type '{0}' to type '{1}'.",
				                                               JNIEnv.GetClassNameFromInstance(handle), "android.hardware.Camera.PreviewCallback"));
			return handle;
		}

		protected override void Dispose(bool disposing)
		{
			if (class_ref != IntPtr.Zero)
				JNIEnv.DeleteGlobalRef(class_ref);
			class_ref = IntPtr.Zero;
			base.Dispose(disposing);
		}

		public INonMarshalingPreviewCallbackInvoker(IntPtr handle, JniHandleOwnership transfer)
			: base(Validate(handle), transfer)
		{
			IntPtr local_ref = JNIEnv.GetObjectClass(Handle);
			class_ref = JNIEnv.NewGlobalRef(local_ref);
			JNIEnv.DeleteLocalRef(local_ref);
		}

		protected override IntPtr ThresholdClass {
			get { return class_ref; }
		}

		protected override Type ThresholdType {
			get { return typeof(INonMarshalingPreviewCallbackInvoker); }
		}

		static Delegate cb_onPreviewFrame_arrayBLandroid_hardware_Camera_;
		#pragma warning disable 0169
		static Delegate GetOnPreviewFrame_arrayBLandroid_hardware_Camera_Handler()
		{
			if (cb_onPreviewFrame_arrayBLandroid_hardware_Camera_ == null)
				cb_onPreviewFrame_arrayBLandroid_hardware_Camera_ = JNINativeWrapper.CreateDelegate((Action<IntPtr, IntPtr, IntPtr, IntPtr>) n_OnPreviewFrame_arrayBLandroid_hardware_Camera_);
			return cb_onPreviewFrame_arrayBLandroid_hardware_Camera_;
		}

		static void n_OnPreviewFrame_arrayBLandroid_hardware_Camera_(IntPtr jnienv, IntPtr native__this, IntPtr native_data, IntPtr native_camera)
		{
			INonMarshalingPreviewCallback __this = GetObject<INonMarshalingPreviewCallback>(native__this, JniHandleOwnership.DoNotTransfer);
			Camera camera = GetObject<Camera>(native_camera, JniHandleOwnership.DoNotTransfer);
			__this.OnPreviewFrame(native_data, camera);
		}
		#pragma warning restore 0169

		IntPtr id_onPreviewFrame_arrayBLandroid_hardware_Camera_;
		public void OnPreviewFrame(IntPtr data, Camera camera)
		{
			if (id_onPreviewFrame_arrayBLandroid_hardware_Camera_ == IntPtr.Zero)
				id_onPreviewFrame_arrayBLandroid_hardware_Camera_ = JNIEnv.GetMethodID(class_ref, "onPreviewFrame", "([BLandroid/hardware/Camera;)V");
			JNIEnv.CallVoidMethod(Handle, id_onPreviewFrame_arrayBLandroid_hardware_Camera_, new JValue(data), new JValue(camera));
		}
	}
}
