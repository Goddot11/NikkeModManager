/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

#if (UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_WSA || UNITY_WP8 || UNITY_WP8_1)
#define IS_UNITY
#endif

using NikkeModManager.spine_4_0_64.Attachments;
#if WINDOWS_STOREAPP
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace NikkeModManager.spine_4_0_64 {

	/// <summary>
	/// Loads skeleton data in the Spine JSON format.
	/// <para>
	/// JSON is human readable but the binary format is much smaller on disk and faster to load. See <see cref="SkeletonBinary"/>.</para>
	/// <para>
	/// See <a href="http://esotericsoftware.com/spine-json-format">Spine JSON format</a> and
	/// <a href = "http://esotericsoftware.com/spine-loading-skeleton-data#JSON-and-binary-data" > JSON and binary data</a> in the Spine
	/// Runtimes Guide.</para>
	/// </summary>
	public class SkeletonJson : SkeletonLoader {

		public SkeletonJson (AttachmentLoader attachmentLoader)
			: base(attachmentLoader) {
		}

		public SkeletonJson (params Atlas[] atlasArray)
			: base(atlasArray) {
		}

#if !IS_UNITY && WINDOWS_STOREAPP
		private async Task<SkeletonData> ReadFile(string path) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			var file = await folder.GetFileAsync(path).AsTask().ConfigureAwait(false);
			using (var reader = new StreamReader(await file.OpenStreamForReadAsync().ConfigureAwait(false))) {
				SkeletonData skeletonData = ReadSkeletonData(reader);
				skeletonData.Name = Path.GetFileNameWithoutExtension(path);
				return skeletonData;
			}
		}

		public override SkeletonData ReadSkeletonData (string path) {
			return this.ReadFile(path).Result;
		}
#else
		public override SkeletonData ReadSkeletonData (string path) {
#if WINDOWS_PHONE
			using (var reader = new StreamReader(Microsoft.Xna.Framework.TitleContainer.OpenStream(path))) {
#else
			using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
#endif
				SkeletonData skeletonData = ReadSkeletonData(reader);
				skeletonData.name = Path.GetFileNameWithoutExtension(path);
				return skeletonData;
			}
		}
#endif

		public SkeletonData ReadSkeletonData (TextReader reader) {
			if (reader == null) throw new ArgumentNullException("reader", "reader cannot be null.");

			float scale = this.scale;
			var skeletonData = new SkeletonData();

			var root = Json.Deserialize(reader) as Dictionary<string, Object>;
			if (root == null) throw new Exception("Invalid JSON.");

			// Skeleton.
			if (root.ContainsKey("skeleton")) {
				var skeletonMap = (Dictionary<string, Object>)root["skeleton"];
				skeletonData.hash = (string)skeletonMap["hash"];
				skeletonData.version = (string)skeletonMap["spine"];
				skeletonData.x = GetFloat(skeletonMap, "x", 0);
				skeletonData.y = GetFloat(skeletonMap, "y", 0);
				skeletonData.width = GetFloat(skeletonMap, "width", 0);
				skeletonData.height = GetFloat(skeletonMap, "height", 0);
				skeletonData.fps = GetFloat(skeletonMap, "fps", 30);
				skeletonData.imagesPath = GetString(skeletonMap, "images", null);
				skeletonData.audioPath = GetString(skeletonMap, "audio", null);
			}

			// Bones.
			if (root.ContainsKey("bones")) {
				foreach (Dictionary<string, Object> boneMap in (List<Object>)root["bones"]) {
					BoneData parent = null;
					if (boneMap.ContainsKey("parent")) {
						parent = skeletonData.FindBone((string)boneMap["parent"]);
						if (parent == null)
							throw new Exception("Parent bone not found: " + boneMap["parent"]);
					}
					var data = new BoneData(skeletonData.Bones.Count, (string)boneMap["name"], parent);
					data.length = GetFloat(boneMap, "length", 0) * scale;
					data.x = GetFloat(boneMap, "x", 0) * scale;
					data.y = GetFloat(boneMap, "y", 0) * scale;
					data.rotation = GetFloat(boneMap, "rotation", 0);
					data.scaleX = GetFloat(boneMap, "scaleX", 1);
					data.scaleY = GetFloat(boneMap, "scaleY", 1);
					data.shearX = GetFloat(boneMap, "shearX", 0);
					data.shearY = GetFloat(boneMap, "shearY", 0);

					string tm = GetString(boneMap, "transform", TransformMode.Normal.ToString());
					data.transformMode = (TransformMode)Enum.Parse(typeof(TransformMode), tm, true);
					data.skinRequired = GetBoolean(boneMap, "skin", false);

					skeletonData.bones.Add(data);
				}
			}

			// Slots.
			if (root.ContainsKey("slots")) {
				foreach (Dictionary<string, Object> slotMap in (List<Object>)root["slots"]) {
					var slotName = (string)slotMap["name"];
					var boneName = (string)slotMap["bone"];
					BoneData boneData = skeletonData.FindBone(boneName);
					if (boneData == null) throw new Exception("Slot bone not found: " + boneName);
					var data = new SlotData(skeletonData.Slots.Count, slotName, boneData);

					if (slotMap.ContainsKey("color")) {
						string color = (string)slotMap["color"];
						data.r = ToColor(color, 0);
						data.g = ToColor(color, 1);
						data.b = ToColor(color, 2);
						data.a = ToColor(color, 3);
					}

					if (slotMap.ContainsKey("dark")) {
						var color2 = (string)slotMap["dark"];
						data.r2 = ToColor(color2, 0, 6); // expectedLength = 6. ie. "RRGGBB"
						data.g2 = ToColor(color2, 1, 6);
						data.b2 = ToColor(color2, 2, 6);
						data.hasSecondColor = true;
					}

					data.attachmentName = GetString(slotMap, "attachment", null);
					if (slotMap.ContainsKey("blend"))
						data.blendMode = (BlendMode)Enum.Parse(typeof(BlendMode), (string)slotMap["blend"], true);
					else
						data.blendMode = BlendMode.Normal;
					skeletonData.slots.Add(data);
				}
			}

			// IK constraints.
			if (root.ContainsKey("ik")) {
				foreach (Dictionary<string, Object> constraintMap in (List<Object>)root["ik"]) {
					IkConstraintData data = new IkConstraintData((string)constraintMap["name"]);
					data.order = GetInt(constraintMap, "order", 0);
					data.skinRequired = GetBoolean(constraintMap, "skin", false);

					if (constraintMap.ContainsKey("bones")) {
						foreach (string boneName in (List<Object>)constraintMap["bones"]) {
							BoneData bone = skeletonData.FindBone(boneName);
							if (bone == null) throw new Exception("IK bone not found: " + boneName);
							data.bones.Add(bone);
						}
					}

					string targetName = (string)constraintMap["target"];
					data.target = skeletonData.FindBone(targetName);
					if (data.target == null) throw new Exception("IK target bone not found: " + targetName);
					data.mix = GetFloat(constraintMap, "mix", 1);
					data.softness = GetFloat(constraintMap, "softness", 0) * scale;
					data.bendDirection = GetBoolean(constraintMap, "bendPositive", true) ? 1 : -1;
					data.compress = GetBoolean(constraintMap, "compress", false);
					data.stretch = GetBoolean(constraintMap, "stretch", false);
					data.uniform = GetBoolean(constraintMap, "uniform", false);

					skeletonData.ikConstraints.Add(data);
				}
			}

			// Transform constraints.
			if (root.ContainsKey("transform")) {
				foreach (Dictionary<string, Object> constraintMap in (List<Object>)root["transform"]) {
					TransformConstraintData data = new TransformConstraintData((string)constraintMap["name"]);
					data.order = GetInt(constraintMap, "order", 0);
					data.skinRequired = GetBoolean(constraintMap, "skin", false);

					if (constraintMap.ContainsKey("bones")) {
						foreach (string boneName in (List<Object>)constraintMap["bones"]) {
							BoneData bone = skeletonData.FindBone(boneName);
							if (bone == null) throw new Exception("Transform constraint bone not found: " + boneName);
							data.bones.Add(bone);
						}
					}

					string targetName = (string)constraintMap["target"];
					data.target = skeletonData.FindBone(targetName);
					if (data.target == null) throw new Exception("Transform constraint target bone not found: " + targetName);

					data.local = GetBoolean(constraintMap, "local", false);
					data.relative = GetBoolean(constraintMap, "relative", false);

					data.offsetRotation = GetFloat(constraintMap, "rotation", 0);
					data.offsetX = GetFloat(constraintMap, "x", 0) * scale;
					data.offsetY = GetFloat(constraintMap, "y", 0) * scale;
					data.offsetScaleX = GetFloat(constraintMap, "scaleX", 0);
					data.offsetScaleY = GetFloat(constraintMap, "scaleY", 0);
					data.offsetShearY = GetFloat(constraintMap, "shearY", 0);

					data.mixRotate = GetFloat(constraintMap, "mixRotate", 1);
					data.mixX = GetFloat(constraintMap, "mixX", 1);
					data.mixY = GetFloat(constraintMap, "mixY", data.mixX);
					data.mixScaleX = GetFloat(constraintMap, "mixScaleX", 1);
					data.mixScaleY = GetFloat(constraintMap, "mixScaleY", data.mixScaleX);
					data.mixShearY = GetFloat(constraintMap, "mixShearY", 1);

					skeletonData.transformConstraints.Add(data);
				}
			}

			// Path constraints.
			if (root.ContainsKey("path")) {
				foreach (Dictionary<string, Object> constraintMap in (List<Object>)root["path"]) {
					PathConstraintData data = new PathConstraintData((string)constraintMap["name"]);
					data.order = GetInt(constraintMap, "order", 0);
					data.skinRequired = GetBoolean(constraintMap, "skin", false);

					if (constraintMap.ContainsKey("bones")) {
						foreach (string boneName in (List<Object>)constraintMap["bones"]) {
							BoneData bone = skeletonData.FindBone(boneName);
							if (bone == null) throw new Exception("Path bone not found: " + boneName);
							data.bones.Add(bone);
						}
					}

					string targetName = (string)constraintMap["target"];
					data.target = skeletonData.FindSlot(targetName);
					if (data.target == null) throw new Exception("Path target slot not found: " + targetName);

					data.positionMode = (PositionMode)Enum.Parse(typeof(PositionMode), GetString(constraintMap, "positionMode", "percent"), true);
					data.spacingMode = (SpacingMode)Enum.Parse(typeof(SpacingMode), GetString(constraintMap, "spacingMode", "length"), true);
					data.rotateMode = (RotateMode)Enum.Parse(typeof(RotateMode), GetString(constraintMap, "rotateMode", "tangent"), true);
					data.offsetRotation = GetFloat(constraintMap, "rotation", 0);
					data.position = GetFloat(constraintMap, "position", 0);
					if (data.positionMode == PositionMode.Fixed) data.position *= scale;
					data.spacing = GetFloat(constraintMap, "spacing", 0);
					if (data.spacingMode == SpacingMode.Length || data.spacingMode == SpacingMode.Fixed) data.spacing *= scale;
					data.mixRotate = GetFloat(constraintMap, "mixRotate", 1);
					data.mixX = GetFloat(constraintMap, "mixX", 1);
					data.mixY = GetFloat(constraintMap, "mixY", data.mixX);

					skeletonData.pathConstraints.Add(data);
				}
			}

			// Skins.
			if (root.ContainsKey("skins")) {
				foreach (Dictionary<string, object> skinMap in (List<object>)root["skins"]) {
					Skin skin = new Skin((string)skinMap["name"]);
					if (skinMap.ContainsKey("bones")) {
						foreach (string entryName in (List<Object>)skinMap["bones"]) {
							BoneData bone = skeletonData.FindBone(entryName);
							if (bone == null) throw new Exception("Skin bone not found: " + entryName);
							skin.bones.Add(bone);
						}
					}
					skin.bones.TrimExcess();
					if (skinMap.ContainsKey("ik")) {
						foreach (string entryName in (List<Object>)skinMap["ik"]) {
							IkConstraintData constraint = skeletonData.FindIkConstraint(entryName);
							if (constraint == null) throw new Exception("Skin IK constraint not found: " + entryName);
							skin.constraints.Add(constraint);
						}
					}
					if (skinMap.ContainsKey("transform")) {
						foreach (string entryName in (List<Object>)skinMap["transform"]) {
							TransformConstraintData constraint = skeletonData.FindTransformConstraint(entryName);
							if (constraint == null) throw new Exception("Skin transform constraint not found: " + entryName);
							skin.constraints.Add(constraint);
						}
					}
					if (skinMap.ContainsKey("path")) {
						foreach (string entryName in (List<Object>)skinMap["path"]) {
							PathConstraintData constraint = skeletonData.FindPathConstraint(entryName);
							if (constraint == null) throw new Exception("Skin path constraint not found: " + entryName);
							skin.constraints.Add(constraint);
						}
					}
					skin.constraints.TrimExcess();
					if (skinMap.ContainsKey("attachments")) {
						foreach (KeyValuePair<string, Object> slotEntry in (Dictionary<string, Object>)skinMap["attachments"]) {
							int slotIndex = FindSlotIndex(skeletonData, slotEntry.Key);
							foreach (KeyValuePair<string, Object> entry in ((Dictionary<string, Object>)slotEntry.Value)) {
								try {
									Attachment attachment = ReadAttachment((Dictionary<string, Object>)entry.Value, skin, slotIndex, entry.Key, skeletonData);
									if (attachment != null) skin.SetAttachment(slotIndex, entry.Key, attachment);
								} catch (Exception e) {
									throw new Exception("Error reading attachment: " + entry.Key + ", skin: " + skin, e);
								}
							}
						}
					}
					skeletonData.skins.Add(skin);
					if (skin.name == "default") skeletonData.defaultSkin = skin;
				}
			}

			// Linked meshes.
			for (int i = 0, n = linkedMeshes.Count; i < n; i++) {
				LinkedMesh linkedMesh = linkedMeshes[i];
				Skin skin = linkedMesh.skin == null ? skeletonData.defaultSkin : skeletonData.FindSkin(linkedMesh.skin);
				if (skin == null) throw new Exception("Slot not found: " + linkedMesh.skin);
				Attachment parent = skin.GetAttachment(linkedMesh.slotIndex, linkedMesh.parent);
				if (parent == null) throw new Exception("Parent mesh not found: " + linkedMesh.parent);
				linkedMesh.mesh.DeformAttachment = linkedMesh.inheritDeform ? (VertexAttachment)parent : linkedMesh.mesh;
				linkedMesh.mesh.ParentMesh = (MeshAttachment)parent;
				linkedMesh.mesh.UpdateUVs();
			}
			linkedMeshes.Clear();

			// Events.
			if (root.ContainsKey("events")) {
				foreach (KeyValuePair<string, Object> entry in (Dictionary<string, Object>)root["events"]) {
					var entryMap = (Dictionary<string, Object>)entry.Value;
					var data = new EventData(entry.Key);
					data.Int = GetInt(entryMap, "int", 0);
					data.Float = GetFloat(entryMap, "float", 0);
					data.String = GetString(entryMap, "string", string.Empty);
					data.AudioPath = GetString(entryMap, "audio", null);
					if (data.AudioPath != null) {
						data.Volume = GetFloat(entryMap, "volume", 1);
						data.Balance = GetFloat(entryMap, "balance", 0);
					}
					skeletonData.events.Add(data);
				}
			}

			// Animations.
			if (root.ContainsKey("animations")) {
				foreach (KeyValuePair<string, Object> entry in (Dictionary<string, Object>)root["animations"]) {
					try {
						ReadAnimation((Dictionary<string, Object>)entry.Value, entry.Key, skeletonData);
					} catch (Exception e) {
						throw new Exception("Error reading animation: " + entry.Key + "\n" + e.Message, e);
					}
				}
			}

			skeletonData.bones.TrimExcess();
			skeletonData.slots.TrimExcess();
			skeletonData.skins.TrimExcess();
			skeletonData.events.TrimExcess();
			skeletonData.animations.TrimExcess();
			skeletonData.ikConstraints.TrimExcess();
			return skeletonData;
		}

		private Attachment ReadAttachment (Dictionary<string, Object> map, Skin skin, int slotIndex, string name, SkeletonData skeletonData) {
			float scale = this.scale;
			name = GetString(map, "name", name);

			var typeName = GetString(map, "type", "region");
			var type = (AttachmentType)Enum.Parse(typeof(AttachmentType), typeName, true);

			string path = GetString(map, "path", name);

			switch (type) {
			case AttachmentType.Region:
				RegionAttachment region = attachmentLoader.NewRegionAttachment(skin, name, path);
				if (region == null) return null;
				region.Path = path;
				region.x = GetFloat(map, "x", 0) * scale;
				region.y = GetFloat(map, "y", 0) * scale;
				region.scaleX = GetFloat(map, "scaleX", 1);
				region.scaleY = GetFloat(map, "scaleY", 1);
				region.rotation = GetFloat(map, "rotation", 0);
				region.width = GetFloat(map, "width", 32) * scale;
				region.height = GetFloat(map, "height", 32) * scale;

				if (map.ContainsKey("color")) {
					var color = (string)map["color"];
					region.r = ToColor(color, 0);
					region.g = ToColor(color, 1);
					region.b = ToColor(color, 2);
					region.a = ToColor(color, 3);
				}

				region.UpdateOffset();
				return region;
			case AttachmentType.Boundingbox:
				BoundingBoxAttachment box = attachmentLoader.NewBoundingBoxAttachment(skin, name);
				if (box == null) return null;
				ReadVertices(map, box, GetInt(map, "vertexCount", 0) << 1);
				return box;
			case AttachmentType.Mesh:
			case AttachmentType.Linkedmesh: {
				MeshAttachment mesh = attachmentLoader.NewMeshAttachment(skin, name, path);
				if (mesh == null) return null;
				mesh.Path = path;

				if (map.ContainsKey("color")) {
					var color = (string)map["color"];
					mesh.r = ToColor(color, 0);
					mesh.g = ToColor(color, 1);
					mesh.b = ToColor(color, 2);
					mesh.a = ToColor(color, 3);
				}

				mesh.Width = GetFloat(map, "width", 0) * scale;
				mesh.Height = GetFloat(map, "height", 0) * scale;

				string parent = GetString(map, "parent", null);
				if (parent != null) {
					linkedMeshes.Add(new LinkedMesh(mesh, GetString(map, "skin", null), slotIndex, parent, GetBoolean(map, "deform", true)));
					return mesh;
				}

				float[] uvs = GetFloatArray(map, "uvs", 1);
				ReadVertices(map, mesh, uvs.Length);
				mesh.triangles = GetIntArray(map, "triangles");
				mesh.regionUVs = uvs;
				mesh.UpdateUVs();

				if (map.ContainsKey("hull")) mesh.HullLength = GetInt(map, "hull", 0) << 1;
				if (map.ContainsKey("edges")) mesh.Edges = GetIntArray(map, "edges");
				return mesh;
			}
			case AttachmentType.Path: {
				PathAttachment pathAttachment = attachmentLoader.NewPathAttachment(skin, name);
				if (pathAttachment == null) return null;
				pathAttachment.closed = GetBoolean(map, "closed", false);
				pathAttachment.constantSpeed = GetBoolean(map, "constantSpeed", true);

				int vertexCount = GetInt(map, "vertexCount", 0);
				ReadVertices(map, pathAttachment, vertexCount << 1);

				// potential BOZO see Java impl
				pathAttachment.lengths = GetFloatArray(map, "lengths", scale);
				return pathAttachment;
			}
			case AttachmentType.Point: {
				PointAttachment point = attachmentLoader.NewPointAttachment(skin, name);
				if (point == null) return null;
				point.x = GetFloat(map, "x", 0) * scale;
				point.y = GetFloat(map, "y", 0) * scale;
				point.rotation = GetFloat(map, "rotation", 0);

				//string color = GetString(map, "color", null);
				//if (color != null) point.color = color;
				return point;
			}
			case AttachmentType.Clipping: {
				ClippingAttachment clip = attachmentLoader.NewClippingAttachment(skin, name);
				if (clip == null) return null;

				string end = GetString(map, "end", null);
				if (end != null) {
					SlotData slot = skeletonData.FindSlot(end);
					if (slot == null) throw new Exception("Clipping end slot not found: " + end);
					clip.EndSlot = slot;
				}

				ReadVertices(map, clip, GetInt(map, "vertexCount", 0) << 1);

				//string color = GetString(map, "color", null);
				// if (color != null) clip.color = color;
				return clip;
			}
			}
			return null;
		}

		private void ReadVertices (Dictionary<string, Object> map, VertexAttachment attachment, int verticesLength) {
			attachment.WorldVerticesLength = verticesLength;
			float[] vertices = GetFloatArray(map, "vertices", 1);
			float scale = Scale;
			if (verticesLength == vertices.Length) {
				if (scale != 1) {
					for (int i = 0; i < vertices.Length; i++) {
						vertices[i] *= scale;
					}
				}
				attachment.vertices = vertices;
				return;
			}
			ExposedList<float> weights = new ExposedList<float>(verticesLength * 3 * 3);
			ExposedList<int> bones = new ExposedList<int>(verticesLength * 3);
			for (int i = 0, n = vertices.Length; i < n;) {
				int boneCount = (int)vertices[i++];
				bones.Add(boneCount);
				for (int nn = i + (boneCount << 2); i < nn; i += 4) {
					bones.Add((int)vertices[i]);
					weights.Add(vertices[i + 1] * this.Scale);
					weights.Add(vertices[i + 2] * this.Scale);
					weights.Add(vertices[i + 3]);
				}
			}
			attachment.bones = bones.ToArray();
			attachment.vertices = weights.ToArray();
		}

		private int FindSlotIndex (SkeletonData skeletonData, string slotName) {
			SlotData[] slots = skeletonData.slots.Items;
			for (int i = 0, n = skeletonData.slots.Count; i < n; i++)
				if (slots[i].name == slotName) return i;
			throw new Exception("Slot not found: " + slotName);
		}

		private void ReadAnimation (Dictionary<string, Object> map, string name, SkeletonData skeletonData) {
			var scale = this.scale;
			var timelines = new ExposedList<Timeline>();

			// Slot timelines.
			if (map.ContainsKey("slots")) {
				foreach (KeyValuePair<string, Object> entry in (Dictionary<string, Object>)map["slots"]) {
					string slotName = entry.Key;
					int slotIndex = FindSlotIndex(skeletonData, slotName);
					var timelineMap = (Dictionary<string, Object>)entry.Value;
					foreach (KeyValuePair<string, Object> timelineEntry in timelineMap) {
						var values = (List<Object>)timelineEntry.Value;
						int frames = values.Count;
						if (frames == 0) continue;
						var timelineName = (string)timelineEntry.Key;
						if (timelineName == "attachment") {
							var timeline = new AttachmentTimeline(frames, slotIndex);
							int frame = 0;
							foreach (Dictionary<string, Object> keyMap in values) {
								timeline.SetFrame(frame++, GetFloat(keyMap, "time", 0), (string)keyMap["name"]);
							}
							timelines.Add(timeline);

						} else if (timelineName == "rgba") {
							var timeline = new RGBATimeline(frames, frames << 2, slotIndex);

							var keyMapEnumerator = values.GetEnumerator();
							keyMapEnumerator.MoveNext();
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							float time = GetFloat(keyMap, "time", 0);
							string color = (string)keyMap["color"];
							float r = ToColor(color, 0);
							float g = ToColor(color, 1);
							float b = ToColor(color, 2);
							float a = ToColor(color, 3);
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, a);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;

								float time2 = GetFloat(nextMap, "time", 0);
								color = (string)nextMap["color"];
								float nr = ToColor(color, 0);
								float ng = ToColor(color, 1);
								float nb = ToColor(color, 2);
								float na = ToColor(color, 3);

								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, r, nr, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, g, ng, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, b, nb, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 3, time, time2, a, na, 1);
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								a = na;
								keyMap = nextMap;
							}
							timelines.Add(timeline);

						} else if (timelineName == "rgb") {
							var timeline = new RGBTimeline(frames, frames * 3, slotIndex);

							var keyMapEnumerator = values.GetEnumerator();
							keyMapEnumerator.MoveNext();
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							float time = GetFloat(keyMap, "time", 0);
							string color = (string)keyMap["color"];
							float r = ToColor(color, 0, 6);
							float g = ToColor(color, 1, 6);
							float b = ToColor(color, 2, 6);
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;

								float time2 = GetFloat(nextMap, "time", 0);
								color = (string)nextMap["color"];
								float nr = ToColor(color, 0, 6);
								float ng = ToColor(color, 1, 6);
								float nb = ToColor(color, 2, 6);

								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, r, nr, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, g, ng, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, b, nb, 1);
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								keyMap = nextMap;
							}
							timelines.Add(timeline);

						} else if (timelineName == "alpha") {
							var keyMapEnumerator = values.GetEnumerator();
							keyMapEnumerator.MoveNext();
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new AlphaTimeline(frames, frames, slotIndex), 0, 1));

						} else if (timelineName == "rgba2") {
							var timeline = new RGBA2Timeline(frames, frames * 7, slotIndex);

							var keyMapEnumerator = values.GetEnumerator();
							keyMapEnumerator.MoveNext();
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							float time = GetFloat(keyMap, "time", 0);
							string color = (string)keyMap["light"];
							float r = ToColor(color, 0);
							float g = ToColor(color, 1);
							float b = ToColor(color, 2);
							float a = ToColor(color, 3);
							color = (string)keyMap["dark"];
							float r2 = ToColor(color, 0, 6);
							float g2 = ToColor(color, 1, 6);
							float b2 = ToColor(color, 2, 6);
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, a, r2, g2, b2);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;

								float time2 = GetFloat(nextMap, "time", 0);
								color = (string)nextMap["light"];
								float nr = ToColor(color, 0);
								float ng = ToColor(color, 1);
								float nb = ToColor(color, 2);
								float na = ToColor(color, 3);
								color = (string)nextMap["dark"];
								float nr2 = ToColor(color, 0, 6);
								float ng2 = ToColor(color, 1, 6);
								float nb2 = ToColor(color, 2, 6);

								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, r, nr, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, g, ng, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, b, nb, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 3, time, time2, a, na, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 4, time, time2, r2, nr2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 5, time, time2, g2, ng2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 6, time, time2, b2, nb2, 1);
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								a = na;
								r2 = nr2;
								g2 = ng2;
								b2 = nb2;
								keyMap = nextMap;
							}
							timelines.Add(timeline);

						} else if (timelineName == "rgb2") {
							var timeline = new RGB2Timeline(frames, frames * 6, slotIndex);

							var keyMapEnumerator = values.GetEnumerator();
							keyMapEnumerator.MoveNext();
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							float time = GetFloat(keyMap, "time", 0);
							string color = (string)keyMap["light"];
							float r = ToColor(color, 0, 6);
							float g = ToColor(color, 1, 6);
							float b = ToColor(color, 2, 6);
							color = (string)keyMap["dark"];
							float r2 = ToColor(color, 0, 6);
							float g2 = ToColor(color, 1, 6);
							float b2 = ToColor(color, 2, 6);
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, r2, g2, b2);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;

								float time2 = GetFloat(nextMap, "time", 0);
								color = (string)nextMap["light"];
								float nr = ToColor(color, 0, 6);
								float ng = ToColor(color, 1, 6);
								float nb = ToColor(color, 2, 6);
								color = (string)nextMap["dark"];
								float nr2 = ToColor(color, 0, 6);
								float ng2 = ToColor(color, 1, 6);
								float nb2 = ToColor(color, 2, 6);

								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, r, nr, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, g, ng, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, b, nb, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 3, time, time2, r2, nr2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 4, time, time2, g2, ng2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 5, time, time2, b2, nb2, 1);
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								r2 = nr2;
								g2 = ng2;
								b2 = nb2;
								keyMap = nextMap;
							}
							timelines.Add(timeline);

						} else
							throw new Exception("Invalid timeline type for a slot: " + timelineName + " (" + slotName + ")");
					}
				}
			}

			// Bone timelines.
			if (map.ContainsKey("bones")) {
				foreach (KeyValuePair<string, Object> entry in (Dictionary<string, Object>)map["bones"]) {
					string boneName = entry.Key;
					int boneIndex = -1;
					var bones = skeletonData.bones.Items;
					for (int i = 0, n = skeletonData.bones.Count; i < n; i++) {
						if (bones[i].name == boneName) {
							boneIndex = i;
							break;
						}
					}
					if (boneIndex == -1) throw new Exception("Bone not found: " + boneName);
					var timelineMap = (Dictionary<string, Object>)entry.Value;
					foreach (KeyValuePair<string, Object> timelineEntry in timelineMap) {
						var values = (List<Object>)timelineEntry.Value;
						var keyMapEnumerator = values.GetEnumerator();
						if (!keyMapEnumerator.MoveNext()) continue;
						int frames = values.Count;
						var timelineName = (string)timelineEntry.Key;
						if (timelineName == "rotate")
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new RotateTimeline(frames, frames, boneIndex), 0, 1));
						else if (timelineName == "translate") {
							TranslateTimeline timeline = new TranslateTimeline(frames, frames << 1, boneIndex);
							timelines.Add(ReadTimeline(ref keyMapEnumerator, timeline, "x", "y", 0, scale));
						} else if (timelineName == "translatex") {
							timelines
								.Add(ReadTimeline(ref keyMapEnumerator, new TranslateXTimeline(frames, frames, boneIndex), 0, scale));
						} else if (timelineName == "translatey") {
							timelines
								.Add(ReadTimeline(ref keyMapEnumerator, new TranslateYTimeline(frames, frames, boneIndex), 0, scale));
						} else if (timelineName == "scale") {
							ScaleTimeline timeline = new ScaleTimeline(frames, frames << 1, boneIndex);
							timelines.Add(ReadTimeline(ref keyMapEnumerator, timeline, "x", "y", 1, 1));
						} else if (timelineName == "scalex")
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new ScaleXTimeline(frames, frames, boneIndex), 1, 1));
						else if (timelineName == "scaley")
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new ScaleYTimeline(frames, frames, boneIndex), 1, 1));
						else if (timelineName == "shear") {
							ShearTimeline timeline = new ShearTimeline(frames, frames << 1, boneIndex);
							timelines.Add(ReadTimeline(ref keyMapEnumerator, timeline, "x", "y", 0, 1));
						} else if (timelineName == "shearx")
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new ShearXTimeline(frames, frames, boneIndex), 0, 1));
						else if (timelineName == "sheary")
							timelines.Add(ReadTimeline(ref keyMapEnumerator, new ShearYTimeline(frames, frames, boneIndex), 0, 1));
						else
							throw new Exception("Invalid timeline type for a bone: " + timelineName + " (" + boneName + ")");
					}
				}
			}

			// IK constraint timelines.
			if (map.ContainsKey("ik")) {
				foreach (KeyValuePair<string, Object> timelineMap in (Dictionary<string, Object>)map["ik"]) {
					var values = (List<Object>)timelineMap.Value;
					var keyMapEnumerator = values.GetEnumerator();
					if (!keyMapEnumerator.MoveNext()) continue;
					var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
					IkConstraintData constraint = skeletonData.FindIkConstraint(timelineMap.Key);
					IkConstraintTimeline timeline = new IkConstraintTimeline(values.Count, values.Count << 1,
						skeletonData.IkConstraints.IndexOf(constraint));
					float time = GetFloat(keyMap, "time", 0);
					float mix = GetFloat(keyMap, "mix", 1), softness = GetFloat(keyMap, "softness", 0) * scale;
					for (int frame = 0, bezier = 0; ; frame++) {
						timeline.SetFrame(frame, time, mix, softness, GetBoolean(keyMap, "bendPositive", true) ? 1 : -1,
							GetBoolean(keyMap, "compress", false), GetBoolean(keyMap, "stretch", false));
						if (!keyMapEnumerator.MoveNext()) {
							timeline.Shrink(bezier);
							break;
						}
						var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
						float time2 = GetFloat(nextMap, "time", 0);
						float mix2 = GetFloat(nextMap, "mix", 1), softness2 = GetFloat(nextMap, "softness", 0) * scale;
						if (keyMap.ContainsKey("curve")) {
							object curve = keyMap["curve"];
							bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, mix, mix2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, softness, softness2, scale);
						}
						time = time2;
						mix = mix2;
						softness = softness2;
						keyMap = nextMap;
					}
					timelines.Add(timeline);
				}
			}

			// Transform constraint timelines.
			if (map.ContainsKey("transform")) {
				foreach (KeyValuePair<string, Object> timelineMap in (Dictionary<string, Object>)map["transform"]) {
					var values = (List<Object>)timelineMap.Value;
					var keyMapEnumerator = values.GetEnumerator();
					if (!keyMapEnumerator.MoveNext()) continue;
					var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
					TransformConstraintData constraint = skeletonData.FindTransformConstraint(timelineMap.Key);
					TransformConstraintTimeline timeline = new TransformConstraintTimeline(values.Count, values.Count * 6,
						skeletonData.TransformConstraints.IndexOf(constraint));
					float time = GetFloat(keyMap, "time", 0);
					float mixRotate = GetFloat(keyMap, "mixRotate", 1), mixShearY = GetFloat(keyMap, "mixShearY", 1);
					float mixX = GetFloat(keyMap, "mixX", 1), mixY = GetFloat(keyMap, "mixY", mixX);
					float mixScaleX = GetFloat(keyMap, "mixScaleX", 1), mixScaleY = GetFloat(keyMap, "mixScaleY", mixScaleX);
					for (int frame = 0, bezier = 0; ; frame++) {
						timeline.SetFrame(frame, time, mixRotate, mixX, mixY, mixScaleX, mixScaleY, mixShearY);
						if (!keyMapEnumerator.MoveNext()) {
							timeline.Shrink(bezier);
							break;
						}
						var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
						float time2 = GetFloat(nextMap, "time", 0);
						float mixRotate2 = GetFloat(nextMap, "mixRotate", 1), mixShearY2 = GetFloat(nextMap, "mixShearY", 1);
						float mixX2 = GetFloat(nextMap, "mixX", 1), mixY2 = GetFloat(nextMap, "mixY", mixX2);
						float mixScaleX2 = GetFloat(nextMap, "mixScaleX", 1), mixScaleY2 = GetFloat(nextMap, "mixScaleY", mixScaleX2);
						if (keyMap.ContainsKey("curve")) {
							object curve = keyMap["curve"];
							bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, mixRotate, mixRotate2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, mixX, mixX2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, mixY, mixY2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 3, time, time2, mixScaleX, mixScaleX2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 4, time, time2, mixScaleY, mixScaleY2, 1);
							bezier = ReadCurve(curve, timeline, bezier, frame, 5, time, time2, mixShearY, mixShearY2, 1);
						}
						time = time2;
						mixRotate = mixRotate2;
						mixX = mixX2;
						mixY = mixY2;
						mixScaleX = mixScaleX2;
						mixScaleY = mixScaleY2;
						mixScaleX = mixScaleX2;
						keyMap = nextMap;
					}
					timelines.Add(timeline);
				}
			}

			// Path constraint timelines.
			if (map.ContainsKey("path")) {
				foreach (KeyValuePair<string, Object> constraintMap in (Dictionary<string, Object>)map["path"]) {
					PathConstraintData constraint = skeletonData.FindPathConstraint(constraintMap.Key);
					if (constraint == null) throw new Exception("Path constraint not found: " + constraintMap.Key);
					int constraintIndex = skeletonData.pathConstraints.IndexOf(constraint);
					var timelineMap = (Dictionary<string, Object>)constraintMap.Value;
					foreach (KeyValuePair<string, Object> timelineEntry in timelineMap) {
						var values = (List<Object>)timelineEntry.Value;
						var keyMapEnumerator = values.GetEnumerator();
						if (!keyMapEnumerator.MoveNext()) continue;

						int frames = values.Count;
						var timelineName = (string)timelineEntry.Key;
						if (timelineName == "position") {
							CurveTimeline1 timeline = new PathConstraintPositionTimeline(frames, frames, constraintIndex);
							timelines.Add(ReadTimeline(ref keyMapEnumerator, timeline, 0, constraint.positionMode == PositionMode.Fixed ? scale : 1));
						} else if (timelineName == "spacing") {
							CurveTimeline1 timeline = new PathConstraintSpacingTimeline(frames, frames, constraintIndex);
							timelines.Add(ReadTimeline(ref keyMapEnumerator, timeline, 0,
								constraint.spacingMode == SpacingMode.Length || constraint.spacingMode == SpacingMode.Fixed ? scale : 1));
						} else if (timelineName == "mix") {
							PathConstraintMixTimeline timeline = new PathConstraintMixTimeline(frames, frames * 3, constraintIndex);
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							float time = GetFloat(keyMap, "time", 0);
							float mixRotate = GetFloat(keyMap, "mixRotate", 1);
							float mixX = GetFloat(keyMap, "mixX", 1), mixY = GetFloat(keyMap, "mixY", mixX);
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, mixRotate, mixX, mixY);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
								float time2 = GetFloat(nextMap, "time", 0);
								float mixRotate2 = GetFloat(nextMap, "mixRotate", 1);
								float mixX2 = GetFloat(nextMap, "mixX", 1), mixY2 = GetFloat(nextMap, "mixY", mixX2);
								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, mixRotate, mixRotate2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, mixX, mixX2, 1);
									bezier = ReadCurve(curve, timeline, bezier, frame, 2, time, time2, mixY, mixY2, 1);
								}
								time = time2;
								mixRotate = mixRotate2;
								mixX = mixX2;
								mixY = mixY2;
								keyMap = nextMap;
							}
							timelines.Add(timeline);
						}
					}
				}
			}

			// Deform timelines.
			if (map.ContainsKey("deform")) {
				foreach (KeyValuePair<string, Object> deformMap in (Dictionary<string, Object>)map["deform"]) {
					Skin skin = skeletonData.FindSkin(deformMap.Key);
					foreach (KeyValuePair<string, Object> slotMap in (Dictionary<string, Object>)deformMap.Value) {
						int slotIndex = FindSlotIndex(skeletonData, slotMap.Key);
						foreach (KeyValuePair<string, Object> timelineMap in (Dictionary<string, Object>)slotMap.Value) {
							var values = (List<Object>)timelineMap.Value;
							var keyMapEnumerator = values.GetEnumerator();
							if (!keyMapEnumerator.MoveNext()) continue;
							var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
							VertexAttachment attachment = (VertexAttachment)skin.GetAttachment(slotIndex, timelineMap.Key);
							if (attachment == null) throw new Exception("Deform attachment not found: " + timelineMap.Key);
							bool weighted = attachment.bones != null;
							float[] vertices = attachment.vertices;
							int deformLength = weighted ? (vertices.Length / 3) << 1 : vertices.Length;
							DeformTimeline timeline = new DeformTimeline(values.Count, values.Count, slotIndex, attachment);
							float time = GetFloat(keyMap, "time", 0);
							for (int frame = 0, bezier = 0; ; frame++) {
								float[] deform;
								if (!keyMap.ContainsKey("vertices")) {
									deform = weighted ? new float[deformLength] : vertices;
								} else {
									deform = new float[deformLength];
									int start = GetInt(keyMap, "offset", 0);
									float[] verticesValue = GetFloatArray(keyMap, "vertices", 1);
									Array.Copy(verticesValue, 0, deform, start, verticesValue.Length);
									if (scale != 1) {
										for (int i = start, n = i + verticesValue.Length; i < n; i++)
											deform[i] *= scale;
									}

									if (!weighted) {
										for (int i = 0; i < deformLength; i++)
											deform[i] += vertices[i];
									}
								}

								timeline.SetFrame(frame, time, deform);
								if (!keyMapEnumerator.MoveNext()) {
									timeline.Shrink(bezier);
									break;
								}
								var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
								float time2 = GetFloat(nextMap, "time", 0);
								if (keyMap.ContainsKey("curve")) {
									object curve = keyMap["curve"];
									bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, 0, 1, 1);
								}
								time = time2;
								keyMap = nextMap;
							}
							timelines.Add(timeline);
						}
					}
				}
			}

			// Draw order timeline.
			if (map.ContainsKey("drawOrder")) {
				var values = (List<Object>)map["drawOrder"];
				var timeline = new DrawOrderTimeline(values.Count);
				int slotCount = skeletonData.slots.Count;
				int frame = 0;
				foreach (Dictionary<string, Object> drawOrderMap in values) {
					int[] drawOrder = null;
					if (drawOrderMap.ContainsKey("offsets")) {
						drawOrder = new int[slotCount];
						for (int i = slotCount - 1; i >= 0; i--)
							drawOrder[i] = -1;
						var offsets = (List<Object>)drawOrderMap["offsets"];
						int[] unchanged = new int[slotCount - offsets.Count];
						int originalIndex = 0, unchangedIndex = 0;
						foreach (Dictionary<string, Object> offsetMap in offsets) {
							int slotIndex = FindSlotIndex(skeletonData, (string)offsetMap["slot"]);
							// Collect unchanged items.
							while (originalIndex != slotIndex)
								unchanged[unchangedIndex++] = originalIndex++;
							// Set changed items.
							int index = originalIndex + (int)(float)offsetMap["offset"];
							drawOrder[index] = originalIndex++;
						}
						// Collect remaining unchanged items.
						while (originalIndex < slotCount)
							unchanged[unchangedIndex++] = originalIndex++;
						// Fill in unchanged items.
						for (int i = slotCount - 1; i >= 0; i--)
							if (drawOrder[i] == -1) drawOrder[i] = unchanged[--unchangedIndex];
					}
					timeline.SetFrame(frame, GetFloat(drawOrderMap, "time", 0), drawOrder);
					++frame;
				}
				timelines.Add(timeline);
			}

			// Event timeline.
			if (map.ContainsKey("events")) {
				var eventsMap = (List<Object>)map["events"];
				var timeline = new EventTimeline(eventsMap.Count);
				int frame = 0;
				foreach (Dictionary<string, Object> eventMap in eventsMap) {
					EventData eventData = skeletonData.FindEvent((string)eventMap["name"]);
					if (eventData == null) throw new Exception("Event not found: " + eventMap["name"]);
					var e = new Event(GetFloat(eventMap, "time", 0), eventData) {
						intValue = GetInt(eventMap, "int", eventData.Int),
						floatValue = GetFloat(eventMap, "float", eventData.Float),
						stringValue = GetString(eventMap, "string", eventData.String)
					};
					if (e.data.AudioPath != null) {
						e.volume = GetFloat(eventMap, "volume", eventData.Volume);
						e.balance = GetFloat(eventMap, "balance", eventData.Balance);
					}
					timeline.SetFrame(frame, e);
					++frame;
				}
				timelines.Add(timeline);
			}
			timelines.TrimExcess();
			float duration = 0;
			var items = timelines.Items;
			for (int i = 0, n = timelines.Count; i < n; i++)
				duration = Math.Max(duration, items[i].Duration);
			skeletonData.animations.Add(new Animation(name, timelines, duration));
		}

		static Timeline ReadTimeline (ref List<object>.Enumerator keyMapEnumerator, CurveTimeline1 timeline, float defaultValue, float scale) {
			var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
			float time = GetFloat(keyMap, "time", 0);
			float value = GetFloat(keyMap, "value", defaultValue) * scale;
			for (int frame = 0, bezier = 0; ; frame++) {
				timeline.SetFrame(frame, time, value);
				if (!keyMapEnumerator.MoveNext()) {
					timeline.Shrink(bezier);
					return timeline;
				}
				var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
				float time2 = GetFloat(nextMap, "time", 0);
				float value2 = GetFloat(nextMap, "value", defaultValue) * scale;
				if (keyMap.ContainsKey("curve")) {
					object curve = keyMap["curve"];
					bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, value, value2, scale);
				}
				time = time2;
				value = value2;
				keyMap = nextMap;
			}
		}

		static Timeline ReadTimeline (ref List<object>.Enumerator keyMapEnumerator, CurveTimeline2 timeline, String name1, String name2, float defaultValue,
			float scale) {

			var keyMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
			float time = GetFloat(keyMap, "time", 0);
			float value1 = GetFloat(keyMap, name1, defaultValue) * scale, value2 = GetFloat(keyMap, name2, defaultValue) * scale;
			for (int frame = 0, bezier = 0; ; frame++) {
				timeline.SetFrame(frame, time, value1, value2);
				if (!keyMapEnumerator.MoveNext()) {
					timeline.Shrink(bezier);
					return timeline;
				}
				var nextMap = (Dictionary<string, Object>)keyMapEnumerator.Current;
				float time2 = GetFloat(nextMap, "time", 0);
				float nvalue1 = GetFloat(nextMap, name1, defaultValue) * scale, nvalue2 = GetFloat(nextMap, name2, defaultValue) * scale;
				if (keyMap.ContainsKey("curve")) {
					object curve = keyMap["curve"];
					bezier = ReadCurve(curve, timeline, bezier, frame, 0, time, time2, value1, nvalue1, scale);
					bezier = ReadCurve(curve, timeline, bezier, frame, 1, time, time2, value2, nvalue2, scale);
				}
				time = time2;
				value1 = nvalue1;
				value2 = nvalue2;
				keyMap = nextMap;
			}
		}

		static int ReadCurve (object curve, CurveTimeline timeline, int bezier, int frame, int value, float time1, float time2,
			float value1, float value2, float scale) {

			string curveString = curve as string;
			if (curveString != null) {
				if (curveString == "stepped") timeline.SetStepped(frame);
				return bezier;
			}
			var curveValues = (List<object>)curve;
			int i = value << 2;
			float cx1 = (float)curveValues[i];
			float cy1 = (float)curveValues[i + 1] * scale;
			float cx2 = (float)curveValues[i + 2];
			float cy2 = (float)curveValues[i + 3] * scale;
			SetBezier(timeline, frame, value, bezier, time1, value1, cx1, cy1, cx2, cy2, time2, value2);
			return bezier + 1;
		}

		static void SetBezier (CurveTimeline timeline, int frame, int value, int bezier, float time1, float value1, float cx1, float cy1,
			float cx2, float cy2, float time2, float value2) {
			timeline.SetBezier(bezier, frame, value, time1, value1, cx1, cy1, cx2, cy2, time2, value2);
		}

		static float[] GetFloatArray (Dictionary<string, Object> map, string name, float scale) {
			var list = (List<Object>)map[name];
			var values = new float[list.Count];
			if (scale == 1) {
				for (int i = 0, n = list.Count; i < n; i++)
					values[i] = (float)list[i];
			} else {
				for (int i = 0, n = list.Count; i < n; i++)
					values[i] = (float)list[i] * scale;
			}
			return values;
		}

		static int[] GetIntArray (Dictionary<string, Object> map, string name) {
			var list = (List<Object>)map[name];
			var values = new int[list.Count];
			for (int i = 0, n = list.Count; i < n; i++)
				values[i] = (int)(float)list[i];
			return values;
		}

		static float GetFloat (Dictionary<string, Object> map, string name, float defaultValue) {
			if (!map.ContainsKey(name)) return defaultValue;
			return (float)map[name];
		}

		static int GetInt (Dictionary<string, Object> map, string name, int defaultValue) {
			if (!map.ContainsKey(name)) return defaultValue;
			return (int)(float)map[name];
		}

		static bool GetBoolean (Dictionary<string, Object> map, string name, bool defaultValue) {
			if (!map.ContainsKey(name)) return defaultValue;
			return (bool)map[name];
		}

		static string GetString (Dictionary<string, Object> map, string name, string defaultValue) {
			if (!map.ContainsKey(name)) return defaultValue;
			return (string)map[name];
		}

		static float ToColor (string hexString, int colorIndex, int expectedLength = 8) {
			if (hexString.Length != expectedLength)
				throw new ArgumentException("Color hexidecimal length must be " + expectedLength + ", recieved: " + hexString, "hexString");
			return Convert.ToInt32(hexString.Substring(colorIndex * 2, 2), 16) / (float)255;
		}
	}
}
