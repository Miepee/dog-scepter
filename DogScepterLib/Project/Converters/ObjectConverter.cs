﻿using DogScepterLib.Core;
using DogScepterLib.Core.Chunks;
using DogScepterLib.Core.Models;
using DogScepterLib.Project.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogScepterLib.Project.Converters
{
    public class ObjectConverter : AssetConverter<AssetObject>
    {
        public override void ConvertData(ProjectFile pf, int index)
        {
            // TODO replace both of these to use the AssetRef list instead once the respective assets are loaded literally at all
            var dataSprites = ((GMChunkSPRT)pf.DataHandle.Chunks["SPRT"]).List;
            var dataCode = ((GMChunkCODE)pf.DataHandle.Chunks["CODE"])?.List;

            GMObject asset = (GMObject)pf.Objects[index].DataAsset;

            AssetObject projectAsset = new AssetObject()
            {
                Name = asset.Name.Content,
                Sprite = asset.SpriteID >= 0 ? dataSprites[asset.SpriteID].Name.Content : null,
                Visible = asset.Visible,
                Solid = asset.Solid,
                Depth = asset.Depth,
                Persistent = asset.Persistent,
                ParentObject = asset.ParentObjectID >= 0 ? pf.Objects[asset.ParentObjectID].Name
                                    : (asset.ParentObjectID == -100 ? "<undefined>" : null),
                MaskSprite = asset.MaskSpriteID >= 0 ? dataSprites[asset.MaskSpriteID].Name.Content : null,
                Physics = asset.Physics,
                PhysicsSensor = asset.PhysicsSensor,
                PhysicsShape = asset.PhysicsShape,
                PhysicsDensity = asset.PhysicsDensity,
                PhysicsRestitution = asset.PhysicsRestitution,
                PhysicsGroup = asset.PhysicsGroup,
                PhysicsLinearDamping = asset.PhysicsLinearDamping,
                PhysicsAngularDamping = asset.PhysicsAngularDamping,
                PhysicsVertices = new List<AssetObject.PhysicsVertex>(),
                PhysicsFriction = asset.PhysicsFriction,
                PhysicsAwake = asset.PhysicsAwake,
                PhysicsKinematic = asset.PhysicsKinematic,
                Events = new SortedDictionary<AssetObject.EventType, List<AssetObject.Event>>()
            };

            pf.Objects[index].Asset = projectAsset;

            foreach (GMObject.PhysicsVertex v in asset.PhysicsVertices)
                projectAsset.PhysicsVertices.Add(new AssetObject.PhysicsVertex() { X = v.X, Y = v.Y });
            for (int j = 0; j < asset.Events.Count; j++)
            {
                List<AssetObject.Event> projectEvents = new List<AssetObject.Event>();
                AssetObject.EventType type = (AssetObject.EventType)j;
                projectAsset.Events[type] = projectEvents;

                void addActions(GMObject.Event ev, AssetObject.Event newEv)
                {
                    foreach (var ac in ev.Actions)
                    {
                        newEv.Actions.Add(new AssetObject.Action()
                        {
                            Code = (dataCode == null) ? ac.CodeID.ToString() : dataCode[ac.CodeID].Name.Content,
                            ID = ac.ID,
                            UseApplyTo = ac.UseApplyTo,
                            ActionName = ac.ActionName?.Content,
                            ArgumentCount = ac.ArgumentCount
                        });
                    }
                }

                switch (type)
                {
                    case AssetObject.EventType.Alarm:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventAlarm()
                            {
                                Actions = new List<AssetObject.Action>(),
                                AlarmNumber = ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Step:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventStep()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeStep = (AssetObject.EventStep.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Collision:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventCollision()
                            {
                                Actions = new List<AssetObject.Action>(),
                                ObjectName = pf.Objects[ev.Subtype].Name
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Keyboard:
                    case AssetObject.EventType.KeyPress:
                    case AssetObject.EventType.KeyRelease:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventKeyboard()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeKey = (AssetObject.EventKeyboard.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Mouse:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventMouse()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeMouse = (AssetObject.EventMouse.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Other:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventOther()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeOther = (AssetObject.EventOther.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Draw:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventDraw()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeDraw = (AssetObject.EventDraw.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    case AssetObject.EventType.Gesture:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventGesture()
                            {
                                Actions = new List<AssetObject.Action>(),
                                SubtypeGesture = (AssetObject.EventGesture.Subtype)ev.Subtype
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                    default:
                        foreach (var ev in asset.Events[j])
                        {
                            var newEv = new AssetObject.EventNormal()
                            {
                                Actions = new List<AssetObject.Action>()
                            };
                            addActions(ev, newEv);
                            projectEvents.Add(newEv);
                        }
                        break;
                }
            }
        }

        public override void ConvertData(ProjectFile pf)
        {
            EmptyRefsForNamed(pf.DataHandle.GetChunk<GMChunkOBJT>().List, pf.Objects);
        }

        public override void ConvertProject(ProjectFile pf)
        {
            var dataAssets = pf.DataHandle.GetChunk<GMChunkOBJT>().List;

            // TODO use refs once added
            GMList<GMSprite> dataSprites = ((GMChunkSPRT)pf.DataHandle.Chunks["SPRT"]).List;
            GMList<GMCode> dataCode = ((GMChunkCODE)pf.DataHandle.Chunks["CODE"]).List;

            int getSprite(string name)
            {
                if (name == null)
                    return -1;
                try
                {
                    return dataSprites.Select((elem, index) => new { elem, index }).First(p => p.elem.Name.Content == name).index;
                }
                catch (InvalidOperationException)
                {
                    return -1;
                }
            }
            int getCode(string name)
            {
                try
                {
                    return dataCode.Select((elem, index) => new { elem, index }).First(p => p.elem.Name.Content == name).index;
                }
                catch (InvalidOperationException)
                {
                    return -1;
                }
            }
            int getObject(string name)
            {
                if (name == null)
                    return -1;
                if (name == "<undefined>")
                    return -100;
                try
                {
                    return pf.Objects.Select((elem, index) => new { elem, index }).First(p => p.elem.Name == name).index;
                }
                catch (InvalidOperationException)
                {
                    return -1;
                }
            }

            List<GMObject> newList = new List<GMObject>();
            for (int i = 0; i < pf.Objects.Count; i++)
            {
                AssetObject projectAsset = pf.Objects[i].Asset;
                if (projectAsset == null)
                {
                    // This asset was never converted, so handle references and re-add it
                    GMObject o = (GMObject)pf.Objects[i].DataAsset;
                    o.Name = pf.DataHandle.DefineString(o.Name.Content);
                    foreach (var evList in o.Events)
                    {
                        foreach (var ev in evList)
                        {
                            foreach (var ac in ev.Actions)
                            {
                                if (ac.ActionName != null)
                                    ac.ActionName = pf.DataHandle.DefineString(ac.ActionName.Content);
                            }
                        }
                    }
                    newList.Add(o);
                    continue;
                }

                GMObject dataAsset = new GMObject()
                {
                    Name = pf.DataHandle.DefineString(projectAsset.Name),
                    SpriteID = getSprite(projectAsset.Sprite),
                    Visible = projectAsset.Visible,
                    Solid = projectAsset.Solid,
                    Depth = projectAsset.Depth,
                    Persistent = projectAsset.Persistent,
                    ParentObjectID = getObject(projectAsset.ParentObject),
                    MaskSpriteID = getSprite(projectAsset.MaskSprite),
                    Physics = projectAsset.Physics,
                    PhysicsSensor = projectAsset.PhysicsSensor,
                    PhysicsShape = projectAsset.PhysicsShape,
                    PhysicsDensity = projectAsset.PhysicsDensity,
                    PhysicsRestitution = projectAsset.PhysicsRestitution,
                    PhysicsGroup = projectAsset.PhysicsGroup,
                    PhysicsLinearDamping = projectAsset.PhysicsLinearDamping,
                    PhysicsAngularDamping = projectAsset.PhysicsAngularDamping,
                    PhysicsVertices = new List<GMObject.PhysicsVertex>(),
                    PhysicsFriction = projectAsset.PhysicsFriction,
                    PhysicsAwake = projectAsset.PhysicsAwake,
                    PhysicsKinematic = projectAsset.PhysicsKinematic,
                    Events = new GMPointerList<GMPointerList<GMObject.Event>>()
                };

                foreach (AssetObject.PhysicsVertex v in projectAsset.PhysicsVertices)
                    dataAsset.PhysicsVertices.Add(new GMObject.PhysicsVertex() { X = v.X, Y = v.Y });

                foreach (var events in projectAsset.Events.Values)
                {
                    var newEvents = new GMPointerList<GMObject.Event>();
                    foreach (var ev in events)
                    {
                        GMObject.Event newEv = new GMObject.Event()
                        {
                            Subtype = 0,
                            Actions = new GMPointerList<GMObject.Event.Action>()
                            {
                                new GMObject.Event.Action()
                                {
                                    LibID = 1,
                                    ID = ev.Actions[0].ID,
                                    Kind = 7,
                                    UseRelative = false,
                                    IsQuestion = false,
                                    UseApplyTo = ev.Actions[0].UseApplyTo,
                                    ExeType = 2,
                                    ActionName = ev.Actions[0].ActionName != null ? pf.DataHandle.DefineString(ev.Actions[0].ActionName) : null,
                                    CodeID = getCode(ev.Actions[0].Code),
                                    ArgumentCount = ev.Actions[0].ArgumentCount,
                                    Who = -1,
                                    Relative = false,
                                    IsNot = false
                                }
                            }
                        };

                        // Handle subtype
                        switch (ev)
                        {
                            case AssetObject.EventAlarm e:
                                newEv.Subtype = e.AlarmNumber;
                                break;
                            case AssetObject.EventStep e:
                                newEv.Subtype = (int)e.SubtypeStep;
                                break;
                            case AssetObject.EventCollision e:
                                newEv.Subtype = getObject(e.ObjectName);
                                break;
                            case AssetObject.EventKeyboard e:
                                newEv.Subtype = (int)e.SubtypeKey;
                                break;
                            case AssetObject.EventMouse e:
                                newEv.Subtype = (int)e.SubtypeMouse;
                                break;
                            case AssetObject.EventOther e:
                                newEv.Subtype = (int)e.SubtypeOther;
                                break;
                            case AssetObject.EventDraw e:
                                newEv.Subtype = (int)e.SubtypeDraw;
                                break;
                            case AssetObject.EventGesture e:
                                newEv.Subtype = (int)e.SubtypeGesture;
                                break;
                        }
                        newEvents.Add(newEv);
                    }
                    dataAsset.Events.Add(newEvents);
                }

                newList.Add(dataAsset);
            }

            dataAssets.Clear();
            foreach (var obj in newList)
                dataAssets.Add(obj);
        }
    }
}
