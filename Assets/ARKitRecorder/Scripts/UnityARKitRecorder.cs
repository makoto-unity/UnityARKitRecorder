using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.XR.iOS;
using UnityEngine.XR.iOS.Utils;

public class UnityARKitRecorder : MonoBehaviour
{
	enum CallMethodTypeId
	{
		SendInitToPlayer,
		UpdateCameraFrame,
		AddFaceAnchor,
		UpdateFaceAnchor,
		RemoveFaceAnchor,
		ReceiveRemoteScreenYTex,
		ReceiveRemoteScreenUVTex,
	}

	[Serializable]
	class FaceTrackingData
	{
		public int TimeCount;
		public CallMethodTypeId TypeId;
		public MessageEventArgs Data;
	}

	[Header("AR FaceTracking Config Options")]
	public bool EnableLightEstimation = true;

	[Header("Run Options")]
	public bool ResetTracking = true;
	public bool RemoveExistingAnchors = true;

	private EditorConnection _editorConnection ;

	private int _currentPlayerId = -1;
	private string _guiMessage = "none";

	private Texture2D _remoteScreenYTex;
	private Texture2D _remoteScreenUvTex;

	private bool _bTexturesInitialized;

	enum RecordingMode
	{
		NONE,    // Do nothing. you can see the video through ARKitRemote 
		RECORDING, // At first, you should record the data with ARKitRemote.
		PLAYING,   // And then, you can play the video.
	}
	[SerializeField] private RecordingMode _recordingMode = RecordingMode.NONE;
	private StreamWriter _writer;
	private StreamReader _reader;
	[SerializeField] private string _saveFilePath = "faceTrackingData.bytes";
	private string _savePath;
	private int _timeCount = 0;
	private bool _isStart = false;
	private FileStream _playingFileStream;
	private BinaryFormatter _playingBinaryFormatter;
	private FaceTrackingData _nowFaceTrackingData;


	// スタート時に呼ばれる
	void Start () 
	{
		_bTexturesInitialized = false;

		_editorConnection = EditorConnection.instance;
		_editorConnection.Initialize ();
		_editorConnection.RegisterConnection (PlayerConnected);
		_editorConnection.RegisterDisconnection (PlayerDisconnected);
		_editorConnection.Register (ConnectionMessageIds.updateCameraFrameMsgId, UpdateCameraFrame);
		_editorConnection.Register (ConnectionMessageIds.addFaceAnchorMsgeId, AddFaceAnchor);
		_editorConnection.Register (ConnectionMessageIds.updateFaceAnchorMsgeId, UpdateFaceAnchor);
		_editorConnection.Register (ConnectionMessageIds.removePlaneAnchorMsgeId, RemoveFaceAnchor);
		_editorConnection.Register (ConnectionMessageIds.screenCaptureYMsgId, ReceiveRemoteScreenYTex);
		_editorConnection.Register (ConnectionMessageIds.screenCaptureUVMsgId, ReceiveRemoteScreenUVTex);

		_savePath = Application.dataPath + "/" + _saveFilePath;
		_timeCount = 0;
		if (_recordingMode == RecordingMode.PLAYING)
		{
			_playingFileStream = new FileStream(_savePath, FileMode.Open, FileAccess.Read);
			_playingBinaryFormatter = new BinaryFormatter();

			if (_playingFileStream.CanRead)
			{
				_nowFaceTrackingData = _playingBinaryFormatter.Deserialize(_playingFileStream) as FaceTrackingData;
			}

			_isStart = true;
		}
	}

	private void SaveData(int timeCount, CallMethodTypeId typeId, MessageEventArgs data, bool isFirst)
	{
		if (_recordingMode != RecordingMode.RECORDING) return;

		FaceTrackingData faceTrackingData = new FaceTrackingData();
		faceTrackingData.TimeCount = timeCount;
		faceTrackingData.TypeId = typeId;
		faceTrackingData.Data = data;
		using (FileStream fs = new FileStream(_savePath, isFirst ? FileMode.Create : FileMode.Append, FileAccess.Write))
		{
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(fs, faceTrackingData);
		}
	}

	private void PlayerConnected(int playerID)
	{
		_currentPlayerId = playerID;
	}

	private void OnGUI()
	{

		if (!_bTexturesInitialized) 
		{
			if (_currentPlayerId != -1) {
				_guiMessage = "Connected to ARKit Remote device : " + _currentPlayerId.ToString ();

				if (GUI.Button (new Rect ((Screen.width / 2) - 200, (Screen.height / 2) - 200, 400, 100), "Start Remote ARKit FaceTracking Session")) 
				{
					SendInitToPlayer ();
				}
			} 
			else 
			{
				_guiMessage = "Please connect to player in the console menu";
			}

			GUI.Box (new Rect ((Screen.width / 2) - 200, (Screen.height / 2) + 100, 400, 50), _guiMessage);
		}

	}

	private void PlayerDisconnected(int playerID)
	{
		if (_currentPlayerId == playerID) {
			_currentPlayerId = -1;
		}
	}

	private void OnDestroy()
	{
#if UNITY_2017_1_OR_NEWER
		if(_editorConnection != null) {
			_editorConnection.DisconnectAll ();
		}
#endif
		if (_recordingMode == RecordingMode.PLAYING)
		{
			_playingFileStream.Close();
		}
	}


	private void InitializeTextures(UnityARCamera camera)
	{
		int yWidth = camera.videoParams.yWidth;
		int yHeight = camera.videoParams.yHeight;
		int uvWidth = yWidth / 2;
		int uvHeight = yHeight / 2;
		if (_remoteScreenYTex == null || _remoteScreenYTex.width != yWidth || _remoteScreenYTex.height != yHeight) {
			if (_remoteScreenYTex) {
				Destroy (_remoteScreenYTex);
			}
			_remoteScreenYTex = new Texture2D (yWidth, yHeight, TextureFormat.R8, false, true);
		}
		if (_remoteScreenUvTex == null || _remoteScreenUvTex.width != uvWidth || _remoteScreenUvTex.height != uvHeight) {
			if (_remoteScreenUvTex) {
				Destroy (_remoteScreenUvTex);
			}
			_remoteScreenUvTex = new Texture2D (uvWidth, uvHeight, TextureFormat.RG16, false, true);
		}

		_bTexturesInitialized = true;
	}

	private void UpdateCameraFrame(MessageEventArgs mea)
	{
		serializableUnityARCamera serCamera = mea.data.Deserialize<serializableUnityARCamera> ();

		UnityARCamera scamera = new UnityARCamera ();
		scamera = serCamera;

		InitializeTextures (scamera);

		UnityARSessionNativeInterface.SetStaticCamera (scamera);
		UnityARSessionNativeInterface.RunFrameUpdateCallbacks ();
		SaveData(_timeCount, CallMethodTypeId.UpdateCameraFrame, mea, false);
	}

	private void AddFaceAnchor(MessageEventArgs mea)
	{
		serializableUnityARFaceAnchor serFaceAnchor = mea.data.Deserialize<serializableUnityARFaceAnchor> ();

		ARFaceAnchor arFaceAnchor = serFaceAnchor;
		UnityARSessionNativeInterface.RunAddAnchorCallbacks (arFaceAnchor);
		SaveData(_timeCount, CallMethodTypeId.AddFaceAnchor, mea, false);
	}

	private void UpdateFaceAnchor(MessageEventArgs mea)
	{
		serializableUnityARFaceAnchor serFaceAnchor = mea.data.Deserialize<serializableUnityARFaceAnchor> ();

		ARFaceAnchor arFaceAnchor = serFaceAnchor;
		UnityARSessionNativeInterface.RunUpdateAnchorCallbacks (arFaceAnchor);
		SaveData(_timeCount, CallMethodTypeId.UpdateFaceAnchor, mea, false);
	}

	private void RemoveFaceAnchor(MessageEventArgs mea)
	{
		serializableUnityARFaceAnchor serFaceAnchor = mea.data.Deserialize<serializableUnityARFaceAnchor> ();

		ARFaceAnchor arFaceAnchor = serFaceAnchor;
		UnityARSessionNativeInterface.RunRemoveAnchorCallbacks (arFaceAnchor);
		SaveData(_timeCount, CallMethodTypeId.RemoveFaceAnchor, mea, false);
	}

	private void ReceiveRemoteScreenYTex(MessageEventArgs mea)
	{
		if (!_bTexturesInitialized)
			return;
		_remoteScreenYTex.LoadRawTextureData(CompressionHelper.ByteArrayDecompress(mea.data));
		_remoteScreenYTex.Apply ();
		UnityARVideo arVideo = Camera.main.GetComponent<UnityARVideo>();
		if (arVideo) {
			arVideo.SetYTexure(_remoteScreenYTex);
		}
		SaveData(_timeCount, CallMethodTypeId.ReceiveRemoteScreenYTex, mea, false);
	}

	private void ReceiveRemoteScreenUVTex(MessageEventArgs mea)
	{
		if (!_bTexturesInitialized)
			return;
		_remoteScreenUvTex.LoadRawTextureData(CompressionHelper.ByteArrayDecompress(mea.data));
		_remoteScreenUvTex.Apply ();
		UnityARVideo arVideo = Camera.main.GetComponent<UnityARVideo>();
		if (arVideo) {
			arVideo.SetUVTexure(_remoteScreenUvTex);
		}
		SaveData(_timeCount, CallMethodTypeId.ReceiveRemoteScreenUVTex, mea, false);
	}


	private void SendInitToPlayer()
	{
		serializableFromEditorMessage sfem = new serializableFromEditorMessage ();
		sfem.subMessageId = SubMessageIds.editorInitARKitFaceTracking;
		serializableARSessionConfiguration ssc = new serializableARSessionConfiguration (UnityARAlignment.UnityARAlignmentCamera, UnityARPlaneDetection.None, false, EnableLightEstimation, true); 
		UnityARSessionRunOption roTracking = ResetTracking ? UnityARSessionRunOption.ARSessionRunOptionResetTracking : 0;
		UnityARSessionRunOption roAnchors = RemoveExistingAnchors ? UnityARSessionRunOption.ARSessionRunOptionRemoveExistingAnchors : 0;
		sfem.arkitConfigMsg = new serializableARKitInit (ssc, roTracking | roAnchors);
		SendToPlayer (ConnectionMessageIds.fromEditorARKitSessionMsgId, sfem);
		SaveData(_timeCount, 0, null, true);
		_isStart = true;
	}

	private void SendToPlayer(System.Guid msgId, byte[] data)
	{
		_editorConnection.Send (msgId, data);
	}

	public void SendToPlayer(System.Guid msgId, object serializableObject)
	{
		byte[] arrayToSend = serializableObject.SerializeToByteArray ();
		SendToPlayer (msgId, arrayToSend);
	}

	private void PlayTrackingData(ref FaceTrackingData faceTrackingData)
	{
		switch (faceTrackingData.TypeId)
		{
			case CallMethodTypeId.SendInitToPlayer:
				break;
			case CallMethodTypeId.UpdateCameraFrame:
				UpdateCameraFrame(faceTrackingData.Data);
				break;
			case CallMethodTypeId.AddFaceAnchor:
				AddFaceAnchor(faceTrackingData.Data);
				break;
			case CallMethodTypeId.UpdateFaceAnchor:
				UpdateFaceAnchor(faceTrackingData.Data);
				break;
			case CallMethodTypeId.RemoveFaceAnchor:
				RemoveFaceAnchor(faceTrackingData.Data);
				break;
			case CallMethodTypeId.ReceiveRemoteScreenYTex:
				ReceiveRemoteScreenYTex(faceTrackingData.Data);
				break;
			case CallMethodTypeId.ReceiveRemoteScreenUVTex:
				ReceiveRemoteScreenUVTex(faceTrackingData.Data);
				break;
		}
	}

	void Update()
	{
		if (_isStart)
		{
			if (_recordingMode == RecordingMode.PLAYING )
			{
				while (_playingFileStream.Position < _playingFileStream.Length &&
				       _timeCount >= _nowFaceTrackingData.TimeCount )
				{
					PlayTrackingData(ref _nowFaceTrackingData);
					_nowFaceTrackingData = _playingBinaryFormatter.Deserialize(_playingFileStream) as FaceTrackingData;
				}			
			}
			
			_timeCount++;
		}
	}
	
}
