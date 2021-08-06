using BuildVerse;
using HarmonyLib;
using HighlightingSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Creativerse_Gloves.Patches
{


	[HarmonyPatch]
	public static class Patches
	{
		public static string ReachCM = "20";
		public static string ReachTP = "15";
		public static string ReachSM = "4";
		//REACH
		[HarmonyPatch(typeof(Builder), "UpdateCursorHits")]
		private static bool Prefix(Builder __instance)
		{
			for (int i = 0; i < 2; i++)
			{
				__instance._cursorBlock[i] = default(Builder.CursorBlockInfo);
			}
			Entity owner = Player.Local.EntityRef;
			EntityComponent.DebugComponents componentDebug = owner.WorldComponent.ComponentDebug;
			Vector3 position = __instance.cameraTrans.position;
			Vector3? vector = null;
			bool flag = CharacterViewManager.GetForMainPlayer().State == CharacterViewManager.ViewState.Third;
			ItemStack quickSelection = Player.Local.EntityRef.GetInventory(false).QuickSelection;
			ProtoItem building = (quickSelection != null) ? quickSelection.ProtoItem : null;
			BlockData blockData = default(BlockData);
			float num = owner.CreatorMode ? (float)Int32.Parse(ReachCM) : ((flag ? (float)Int32.Parse(ReachTP) : (float)Int32.Parse(ReachSM) + 2f));
			if (num > 0f)
			{
				Ray ray = new Ray(__instance.cameraTrans.position, __instance.cameraTrans.forward);
				vector = MapRayIntersection.BlueprintIntersection(__instance.map, ray, num - 0.5f, out blockData, true, building, 0.5f);
				if (vector != null)
				{
					__instance._cursorBlock[1].Inside = new Vector3i?(vector.Value.ToVector3iRounded());
					__instance._cursorBlock[1].Outside = new Vector3i?(vector.Value.ToVector3iRounded());
				}
			}
			bool fluids = false;
			Ray ray2 = new Ray(position, __instance.cameraTrans.forward);
			Entity entity = null;
			GameObject hitGameObj = null;
			int hitMask = MapRayIntersection.PickingMask | MapRayIntersection.HitBoxMask;
			MapRayIntersection.Result result;
			if (MapRayIntersection.Intersection(out result, __instance.map, ray2, owner.CreatorMode ? (float)Int32.Parse(ReachCM) : (flag ? (float)Int32.Parse(ReachTP) : (float)Int32.Parse(ReachSM)), hitMask, fluids, true, false, true, true, null, false, null, false, false))
			{
				__instance._cursorBlock[0].Inside = new Vector3i?(result.HitBlockInner);
				__instance._cursorBlock[0].Outside = new Vector3i?(result.HitBlockOuter);
				__instance._cursorBlock[0].FaceDirection = result.HitBlockFace;
				if (vector != null)
				{
					float num2 = Vector3.Distance(position, vector.Value);
					if (Vector3.Distance(position, result.HitPoint) < num2)
					{
						vector = null;
						__instance._cursorBlock[1].Inside = null;
						__instance._cursorBlock[1].Outside = null;
					}
				}
				Vector3i? point = new Vector3i?(result.HitBlockInner);
				Vector3i pos = Map.Instance.ResolveToRootPosition(result.HitBlockInner);
				if (result.HitCollider != null && __instance.FilterEntityHit(result.HitCollider, point, out entity, out hitGameObj))
				{
					__instance._cursorBlock[0].HitEntity = entity;
					__instance._cursorBlock[0].HitGameObj = hitGameObj;
				}
				else
				{
					Map.Instance.GetBlock(pos, false);
					Entity blockEntityByPos = EntityManager.Instance.GetBlockEntityByPos(pos);
					if (blockEntityByPos != null && blockEntityByPos.BlockEntityComponentCached != null && blockEntityByPos.BlockEntityComponentCached.CanInteract())
					{
						entity = blockEntityByPos;
						hitGameObj = entity.WorldComponent.gameObject;
						__instance._cursorBlock[0].HitEntity = entity;
						__instance._cursorBlock[0].HitGameObj = hitGameObj;
					}
				}
			}
			if (__instance._cursor != null)
			{
				CharacterViewManager forMainPlayer = CharacterViewManager.GetForMainPlayer();
				if ((owner.WorldComponent.HandsHidden && !MainUI.Instance.DragDropManager.IsShowingCreatorHud()) || !owner.WorldComponent.HasGauntlet || owner.WorldComponent.IsChannelingProhibited() || MainUI.Instance.CameraHUD.isActiveAndEnabled || (forMainPlayer.State != CharacterViewManager.ViewState.Default && forMainPlayer.State != CharacterViewManager.ViewState.Third))
				{
					__instance.HighlightedLighting = null;
					__instance._cursor.gameObject.SetActive(false);
				}
				else
				{
					Vector3i? vector3i = null;
					__instance._blockFaceDirection = Vector3i.zero;
					bool flag2 = false;
					bool flag3;
					if (__instance._cursorBlock[1].Inside != null)
					{
						flag3 = (flag2 = true);
						vector3i = __instance._cursorBlock[1].Inside;
						__instance._blockFaceDirection = __instance._cursorBlock[1].FaceDirection;
						result.HitRootObject = null;
					}
					else
					{
						vector3i = __instance._cursorBlock[0].Inside;
						__instance._blockFaceDirection = __instance._cursorBlock[0].FaceDirection;
						Entity hitEntity = __instance._cursorBlock[0].HitEntity;
						if (hitEntity == null)
						{
							flag3 = true;
						}
						else
						{
							flag3 = false;
							if (hitEntity.GetSimComponent<NPC>() != null)
							{
								flag3 = true;
							}
						}
					}
					bool flag4 = vector3i != null && vector3i != __instance._lastCursor;
					if (vector3i != null)
					{
						BlockData block = Map.Instance.GetBlock(vector3i.Value, false);
						BlockData blueprintBlock = __instance.GetBlueprintBlock(vector3i, true);
						__instance._lastBlockDir = __instance._blockFaceDirection;
						UpdateWorldLightingFactor updateWorldLightingFactor = null;
						bool flag5 = (!block.IsEmpty && ProtoDatabase.UsePrefab((uint)block.BlockType)) || entity != null;
						if (result.HitRootObject != null)
						{
							UpdateWorldLightingFactor componentInChildren = result.HitRootObject.GetComponentInChildren<UpdateWorldLightingFactor>();
							if (componentInChildren != null)
							{
								updateWorldLightingFactor = componentInChildren;
							}
						}
						else if (blueprintBlock.IsEmpty && flag5)
						{
							Vector3i value = vector3i.Value;
							GameObject prefabFromWorldPos = Map.Instance.GetPrefabFromWorldPos(value);
							if (prefabFromWorldPos != null && prefabFromWorldPos.GetComponent<BakedPrefabPlaceholder>() == null)
							{
								UpdateWorldLightingFactor componentInChildren2 = prefabFromWorldPos.GetComponentInChildren<UpdateWorldLightingFactor>();
								if (componentInChildren2 != null)
								{
									updateWorldLightingFactor = componentInChildren2;
								}
							}
							else
							{
								Builder.BakedHighlight bakedHighlight = default(Builder.BakedHighlight);
								if ((!(__instance._rotatingBlock != null) || !__instance._rotatingBlock.gameObject.activeSelf || __instance._rotatePosition == null || !(__instance._rotatePosition.Value == value)) && (!__instance._highlightBaked.TryGetValue(value, out bakedHighlight) || bakedHighlight.BlockData.RawData != block.RawData))
								{
									if (bakedHighlight.Go != null)
									{
										UnityEngine.Object.Destroy(bakedHighlight.Go);
									}
									block.GetBlockSet();
									new ArrayChunkBuilder();
									new List<int>();
									bakedHighlight.BlockData = block;
									bakedHighlight.Go = ChunkBuilder.GenerateStandaloneBlock(block, value, ChunkBuilder.GenerateFlag.DontUseBakedPrefab);
									if (bakedHighlight.Go != null)
									{
										string name = "_baked_prefab_" + block.ProtoBlock.name;
										bakedHighlight.Go.name = name;
										bakedHighlight.BlockPos = value;
										bakedHighlight.Go.ForEachComponent(delegate (Collider c)
										{
											c.enabled = false;
										});
										bakedHighlight.Go.ForEachComponent(delegate (ParticleSystem c)
										{
											c.Stop();
										});
										bakedHighlight.Go.ForEachComponent(delegate (Light c)
										{
											c.enabled = false;
										});
										bakedHighlight.Go.ForEachComponent(delegate (MeshRenderer c)
										{
											c.GetComponent<MeshFilter>();
											c.shadowCastingMode = ShadowCastingMode.Off;
											c.receiveShadows = false;
											Material[] materials = c.materials;
											for (int j = 0; j < materials.Length; j++)
											{
												Material material = new Material(ProtoDatabaseGameObject.Instance.InvisibleMaterial);
												material.name = string.Format("{0}_submesh{1}", name, j);
												Material material2 = materials[j];
												if (material2.HasProperty(ShaderPropertyID._MainTex))
												{
													material.SetTexture(ShaderPropertyID._MainTex, material2.mainTexture);
													material.SetTextureOffset(ShaderPropertyID._MainTex, material2.mainTextureOffset);
													material.SetTextureScale(ShaderPropertyID._MainTex, material2.mainTextureScale);
												}
												materials[j] = material;
											}
											c.materials = materials;
										});
										bakedHighlight.WorldLighting = bakedHighlight.Go.GetComponentInChildren<UpdateWorldLightingFactor>();
										if (bakedHighlight.WorldLighting == null)
										{
											bakedHighlight.WorldLighting = bakedHighlight.Go.AddComponent<UpdateWorldLightingFactor>();
										}
										__instance._highlightBaked[value] = bakedHighlight;
									}
								}
								if (bakedHighlight.Go != null)
								{
									bakedHighlight.Go.transform.position = value.ToVector3();
									updateWorldLightingFactor = bakedHighlight.WorldLighting;
								}
								if (__instance._miningBlock != null && __instance._miningBlock.isActiveAndEnabled)
								{
									if (updateWorldLightingFactor != null)
									{
										updateWorldLightingFactor.SetTargetedRenderState(null, true);
									}
									updateWorldLightingFactor = null;
								}
							}
						}
						if (updateWorldLightingFactor == null)
						{
							if (!blueprintBlock.IsEmpty)
							{
								__instance._cursor.transform.position = vector3i.Value.ToVector3();
								__instance._cursor.transform.rotation = Quaternion.identity;
								ItemStack quickSelection2 = Player.Local.EntityRef.GetInventory(false).QuickSelection;
								ProtoItemBlock protoItemBlock = ((quickSelection2 == null) ? null : quickSelection2.ProtoItem) as ProtoItemBlock;
								if (flag4 && ((protoItemBlock != null) ? protoItemBlock.GetProtoBlock() : null) == blueprintBlock.ProtoBlock)
								{
									AudioController.Play("BlueprintBlock_HighlightCorrect");
								}
							}
							else
							{
								Vector3 vector2 = vector3i.Value.ToVector3();
								__instance._cursor.transform.localScale = Vector3.one;
								if (Vector3i.up == __instance._blockFaceDirection)
								{
									if (block.ProtoBlock != null)
									{
										vector2.y += block.GetTopSelectionOffset() - 0.5f;
									}
								}
								else if (Vector3i.down != __instance._blockFaceDirection && block.ProtoBlock != null)
								{
									float topSelectionOffset = block.GetTopSelectionOffset();
									vector2.y += topSelectionOffset - (0.5f + topSelectionOffset) * 0.5f;
									__instance._cursor.transform.localScale = new Vector3(1f, 0.5f + topSelectionOffset, 1f);
								}
								vector2 += __instance._blockFaceDirection.ToVector3() * 0.55f;
								__instance._cursor.transform.position = vector2;
								__instance._cursor.transform.rotation = Quaternion.LookRotation(__instance._blockFaceDirection.ToVector3() * -1f);
								__instance._lastCursor = vector3i;
								flag2 = true;
							}
						}
						if (__instance.GetCancelledBlockAt(vector3i.Value) != null)
						{
							flag2 = false;
						}
						__instance.HighlightedLighting = updateWorldLightingFactor;
						__instance.UpdateCursorColor(vector3i, block, blueprintBlock);
					}
					else
					{
						__instance.HighlightedLighting = null;
					}
					__instance._cursor.gameObject.SetActive(flag3 && flag2);
				}
			}
			__instance.UpdateProtoInfo(__instance._cursorBlock[0].Inside, __instance._cursorBlock[1].Inside, __instance._cursorBlock[0].HitEntity);
			__instance.UpdateInteractTarget(__instance._cursorBlock[0].HitEntity, __instance._cursorBlock[0].HitGameObj, __instance._cursorBlock[1].Inside);
			MainUI.Instance.ClearUnsetInfos();
			if (Player.Local.Equipment._slots[4].ProtoItem.Name == "focus_stone")
			{
				ReachSM = "4";
				ReachTP = "10";
			}
			if (Player.Local.Equipment._slots[4].ProtoItem.Name == "focus_obsidian")
			{
				ReachSM = "5";
				ReachTP = "12";
			}
			if (Player.Local.Equipment._slots[4].ProtoItem.Name == "focus_iron")
			{
				ReachSM = "7";
				ReachTP = "14";
			}
			if (Player.Local.Equipment._slots[4].ProtoItem.Name == "focus_diamond")
			{
				ReachSM = "8";
				ReachTP = "16";
			}
			if (Player.Local.Equipment._slots[4].ProtoItem.Name == "focus_lumite")
			{
				ReachSM = "11";
				ReachTP = "18";
			}
			if (Player.Local.Equipment._slots[4].ProtoItem.Name != "focus_lumite" && Player.Local.Equipment._slots[4].ProtoItem.Name != "focus_diamond" && Player.Local.Equipment._slots[4].ProtoItem.Name != "focus_iron" && Player.Local.Equipment._slots[4].ProtoItem.Name != "focus_obsidian" && Player.Local.Equipment._slots[4].ProtoItem.Name != "focus_stone")
			{
				ReachSM = "4";
				ReachTP = "10";
			}
			return false;
		}
    }
}
