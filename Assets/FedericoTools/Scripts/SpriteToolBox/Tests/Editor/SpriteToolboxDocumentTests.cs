using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FedericoTools.SpriteToolBox.Editor;

namespace FedericoTools.SpriteToolBox.Tests.Editor
{
    public sealed class SpriteToolboxDocumentTests
    {
        [Test]
        public void FloodFill_ChangesOnlyConnectedPixels()
        {
            SpriteCel cel = new SpriteCel(3, 2);
            cel.SetPixel(new Vector2Int(1, 0), Color.black);
            List<PixelChange> changes = SpritePixelOperations.CreateFloodFillChanges(cel, new Vector2Int(0, 0), Color.red);

            PixelChangesCommand command = new PixelChangesCommand(cel, changes);
            command.Execute();

            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.black));
        }

        [Test]
        public void FloodFill_ToleranceIncludesOnlyConnectedNearbyColors()
        {
            SpriteCel cel = new SpriteCel(3, 1);
            cel.SetPixel(new Vector2Int(0, 0), new Color32(100, 100, 100, 255));
            cel.SetPixel(new Vector2Int(1, 0), new Color32(106, 100, 100, 255));
            cel.SetPixel(new Vector2Int(2, 0), new Color32(120, 100, 100, 255));

            List<PixelChange> changes = SpritePixelOperations.CreateFloodFillChanges(cel, Vector2Int.zero, Color.red, 6);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[0].Position, Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(changes[1].Position, Is.EqualTo(new Vector2Int(1, 0)));
        }

        [Test]
        public void FloodFill_TreatsFullyTransparentPixelsAsTheSameColor()
        {
            SpriteCel cel = new SpriteCel(3, 1);
            cel.SetPixel(Vector2Int.zero, new Color32(12, 34, 56, 0));
            cel.SetPixel(Vector2Int.right, new Color32(220, 180, 40, 0));
            cel.SetPixel(new Vector2Int(2, 0), Color.black);

            List<PixelChange> changes = SpritePixelOperations.CreateFloodFillChanges(cel, Vector2Int.zero, Color.red);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[0].Position, Is.EqualTo(Vector2Int.zero));
            Assert.That(changes[1].Position, Is.EqualTo(Vector2Int.right));
        }

        [Test]
        public void MagicSelection_UsesFloodFillToleranceOnSelectedLayer()
        {
            SpriteDocument document = new SpriteDocument(3, 1);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), new Color32(100, 100, 100, 255));
            cel.SetPixel(new Vector2Int(1, 0), new Color32(106, 100, 100, 255));
            cel.SetPixel(new Vector2Int(2, 0), new Color32(120, 100, 100, 255));

            session.SetFillTolerance(6);
            session.SetMagicPixelSelection(Vector2Int.zero);

            Assert.That(session.HasPixelSelection, Is.True);
            Assert.That(session.IsMagicPixelSelection, Is.True);
            Assert.That(session.PixelSelectionPositions.Count, Is.EqualTo(2));
        }

        [Test]
        public void MagicSelection_CanAddAndRemoveConnectedRegions()
        {
            SpriteDocument document = new SpriteDocument(5, 1);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), Color.red);
            cel.SetPixel(new Vector2Int(1, 0), Color.red);
            cel.SetPixel(new Vector2Int(2, 0), Color.black);
            cel.SetPixel(new Vector2Int(3, 0), Color.blue);
            cel.SetPixel(new Vector2Int(4, 0), Color.blue);

            session.SetMagicPixelSelection(Vector2Int.zero);
            Assert.That(session.ModifyMagicPixelSelection(new Vector2Int(3, 0), true), Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)
            }));

            Assert.That(session.ModifyMagicPixelSelection(Vector2Int.zero, false), Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[] { new Vector2Int(3, 0), new Vector2Int(4, 0) }));
        }

        [Test]
        public void RectangularSelection_CanAddAndRemoveAreas()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(4, 2));
            session.SetPixelSelectionRect(new RectInt(0, 0, 1, 2));
            session.SetPixelSelectionActive(true);

            Assert.That(session.ModifyPixelSelectionRect(new RectInt(2, 0, 1, 2), true), Is.True);
            Assert.That(session.IsMagicPixelSelection, Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(2, 0), new Vector2Int(2, 1)
            }));

            Assert.That(session.ModifyPixelSelectionRect(new RectInt(0, 0, 1, 2), false), Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[] { new Vector2Int(2, 0), new Vector2Int(2, 1) }));
        }

        [Test]
        public void Session_UndoSelectionRestoresThePreviousSelectionState()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(2, 2));
            session.BeginSelectionUndo();
            session.SetPixelSelectionRect(new RectInt(0, 0, 1, 1));
            session.SetPixelSelectionActive(true);

            Assert.That(session.UndoSelection(), Is.True);
            Assert.That(session.HasPixelSelection, Is.False);
        }

        [Test]
        public void Session_CropCanvasToSelection_CropsToTheSmallestContainingSquareAndIsUndoable()
        {
            SpriteDocument document = new SpriteDocument(5, 4);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(2, 1), Color.red);
            cel.SetPixel(new Vector2Int(3, 2), Color.blue);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SetPixelSelectionRect(new RectInt(2, 1, 2, 2));
            session.SetPixelSelectionActive(true);

            Assert.That(session.CropCanvasToSelection(), Is.True);
            Assert.That(document.Width, Is.EqualTo(2));
            Assert.That(document.Height, Is.EqualTo(2));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(1, 1)), Is.EqualTo(Color.blue));
            Assert.That(session.HasPixelSelection, Is.False);

            session.Undo();
            Assert.That(document.Width, Is.EqualTo(5));
            Assert.That(document.Height, Is.EqualTo(4));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.red));
        }

        [Test]
        public void Renderer_CompositesVisibleLayersInOrder()
        {
            SpriteDocument document = new SpriteDocument(1, 1);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, Color.red);
            document.AddLayerTrack("Top");
            document.GetFrame(0).GetCel(1).SetPixel(Vector2Int.zero, Color.blue);

            Texture2D texture = SpriteDocumentRenderer.RenderFrame(document, 0);
            Assert.That(texture.GetPixel(0, 0), Is.EqualTo(Color.blue));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Blend_PreservesSemiTransparentColorOverTransparentBackground()
        {
            Color32 source = new Color32(221, 119, 73, 128);
            SpriteDocument document = new SpriteDocument(1, 1);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, source);
            Texture2D texture = SpriteDocumentRenderer.RenderFrame(document, 0);
            Color32 result = texture.GetPixel(0, 0);

            Assert.That(result, Is.EqualTo(source));
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void RoundBrush_ExcludesCornersWhileSquareBrushIncludesThem()
        {
            Assert.That(SpriteBrushShapeUtility.ContainsOffset(-1, -1, 3, SpriteBrushShape.Round), Is.False);
            Assert.That(SpriteBrushShapeUtility.ContainsOffset(0, 0, 3, SpriteBrushShape.Round), Is.True);
            Assert.That(SpriteBrushShapeUtility.ContainsOffset(-1, -1, 3, SpriteBrushShape.Square), Is.True);
        }

        [Test]
        public void SpriteSheet_CompositesFramesWithoutChangingTheirOrder()
        {
            SpriteDocument document = new SpriteDocument(1, 1);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, Color.red);
            document.InsertFrame(1, false);
            document.GetFrame(1).GetCel(0).SetPixel(Vector2Int.zero, Color.blue);

            Texture2D spriteSheet = SpriteDocumentRenderer.RenderSpriteSheet(document, 1);

            Assert.That(spriteSheet.GetPixel(0, 1), Is.EqualTo(Color.red));
            Assert.That(spriteSheet.GetPixel(0, 0), Is.EqualTo(Color.blue));
            Object.DestroyImmediate(spriteSheet);
        }

        [Test]
        public void CommandHistory_EvictsOldestUndoWhenBudgetIsExceeded()
        {
            SpriteCel cel = new SpriteCel(1, 1);
            SpriteCommandHistory history = new SpriteCommandHistory(40L);
            history.Execute(new PixelChangesCommand(cel, new List<PixelChange> { new PixelChange(Vector2Int.zero, Color.clear, Color.red) }));
            history.Execute(new PixelChangesCommand(cel, new List<PixelChange> { new PixelChange(Vector2Int.zero, Color.red, Color.blue) }));

            history.Undo();

            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));
            Assert.That(history.CanUndo, Is.False);
        }

        [Test]
        public void GranularLayerAndFrameCommands_RestoreOnlyAffectedData()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            SpriteCommandHistory history = new SpriteCommandHistory();
            history.Execute(new InsertLayerCommand(document, 1, "Top"));
            document.GetFrame(0).GetCel(1).SetPixel(Vector2Int.zero, Color.blue);
            history.Execute(new InsertFrameCommand(document, 1, true, 0.2f));

            Assert.That(document.Frames.Count, Is.EqualTo(2));
            Assert.That(document.LayerTracks.Count, Is.EqualTo(2));
            Assert.That(document.GetFrame(1).GetCel(1).GetPixel(Vector2Int.zero), Is.EqualTo(Color.blue));

            history.Undo();
            history.Undo();

            Assert.That(document.Frames.Count, Is.EqualTo(1));
            Assert.That(document.LayerTracks.Count, Is.EqualTo(1));
        }

        [Test]
        public void MergeLayerDownCommand_RestoresRemovedLayerAndPixels()
        {
            SpriteDocument document = new SpriteDocument(1, 1);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, Color.red);
            document.AddLayerTrack("Top");
            document.GetFrame(0).GetCel(1).SetPixel(Vector2Int.zero, Color.blue);
            SpriteCommandHistory history = new SpriteCommandHistory();
            history.Execute(new MergeLayerDownCommand(document, 1));

            Assert.That(document.LayerTracks.Count, Is.EqualTo(1));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.blue));

            history.Undo();

            Assert.That(document.LayerTracks.Count, Is.EqualTo(2));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));
            Assert.That(document.GetFrame(0).GetCel(1).GetPixel(Vector2Int.zero), Is.EqualTo(Color.blue));
        }

        [Test]
        public void FrameAndLayerOperations_PreserveCelData()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, Color.yellow);
            document.InsertFrame(1, true, 0.2f);
            document.AddLayerTrack("Top");
            document.GetFrame(1).GetCel(1).SetPixel(Vector2Int.zero, Color.green);
            document.MergeLayerDown(1);
            document.MoveFrame(1, 0);

            Assert.That(document.Frames.Count, Is.EqualTo(2));
            Assert.That(document.LayerTracks.Count, Is.EqualTo(1));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.green));
            Assert.That(document.GetFrame(1).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.yellow));
        }

        [Test]
        public void SnapshotCommand_RestoresStructuralChanges()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            SpriteDocument before = document.Clone();
            document.AddLayerTrack("Second");
            SpriteDocument after = document.Clone();
            SpriteCommandHistory history = new SpriteCommandHistory();
            history.Execute(new SpriteDocumentSnapshotCommand(document, before, after));

            history.Undo();
            Assert.That(document.LayerTracks.Count, Is.EqualTo(1));
            history.Redo();
            Assert.That(document.LayerTracks.Count, Is.EqualTo(2));
        }

        [Test]
        public void CanvasCoordinates_MapsGuiTopLeftToDocumentTopRow()
        {
            bool isInside = SpriteCanvasCoordinates.TryGetPixelPosition(new Vector2(21f, 11f), new Rect(10f, 10f, 40f, 30f), 4, 3, 10f, out Vector2Int pixel);

            Assert.That(isInside, Is.True);
            Assert.That(pixel, Is.EqualTo(new Vector2Int(1, 2)));
        }

        [Test]
        public void CanvasCoordinates_UnboundedMappingPreservesOutsideLineEndpoint()
        {
            bool wasMapped = SpriteCanvasCoordinates.TryGetPixelPositionUnbounded(new Vector2(55f, 5f), new Rect(10f, 10f, 40f, 30f), 4, 3, 10f, out Vector2Int pixel);

            Assert.That(wasMapped, Is.True);
            Assert.That(pixel, Is.EqualTo(new Vector2Int(4, 3)));
        }

        [Test]
        public void CanvasPan_UsesStableScreenSpaceDelta()
        {
            Vector2 startScrollPosition = new Vector2(4096f, 4096f);
            Vector2 startMouseScreenPosition = new Vector2(300f, 200f);

            Vector2 firstPosition = SpriteToolboxCanvasPanel.CalculatePannedScrollPosition(startScrollPosition, startMouseScreenPosition, new Vector2(312f, 207f));
            Vector2 secondPosition = SpriteToolboxCanvasPanel.CalculatePannedScrollPosition(startScrollPosition, startMouseScreenPosition, new Vector2(324f, 214f));

            Assert.That(firstPosition, Is.EqualTo(new Vector2(4084f, 4089f)));
            Assert.That(secondPosition, Is.EqualTo(new Vector2(4072f, 4082f)));
            Assert.That(secondPosition - firstPosition, Is.EqualTo(new Vector2(-12f, -7f)));
        }

        [Test]
        public void DocumentAsset_RoundTripsSerializedDocument()
        {
            const string assetPath = "Assets/__SpriteToolboxDocumentTest.asset";
            SpriteDocumentAsset asset = ScriptableObject.CreateInstance<SpriteDocumentAsset>();
            asset.Initialize(3, 2);
            asset.Document.GetFrame(0).GetCel(0).SetPixel(new Vector2Int(2, 1), Color.magenta);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            SpriteDocumentAsset reloaded = AssetDatabase.LoadAssetAtPath<SpriteDocumentAsset>(assetPath);
            Assert.That(reloaded.Document.Width, Is.EqualTo(3));
            Assert.That(reloaded.Document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.magenta));

            AssetDatabase.DeleteAsset(assetPath);
        }

        [Test]
        public void CloneAndRestore_PreserveIndependentAnimationTags()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.AddAnimationTag("Idle", 0, 1);
            document.UpdateAnimationTag(0, "Idle", 0, 1, SpriteAnimationDirection.PingPong, Color.cyan);

            SpriteDocument clone = document.Clone();
            clone.UpdateAnimationTag(0, "Changed", 1, 1, SpriteAnimationDirection.Reverse, Color.magenta);
            document.RestoreFrom(clone);

            Assert.That(document.AnimationTags.Count, Is.EqualTo(1));
            Assert.That(document.AnimationTags[0].Name, Is.EqualTo("Changed"));
            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(1));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(1));
            Assert.That(document.AnimationTags[0].Direction, Is.EqualTo(SpriteAnimationDirection.Reverse));
            Assert.That(document.AnimationTags[0].Color, Is.EqualTo(Color.magenta));
        }

        [Test]
        public void FrameInsertionAndRemoval_KeepAnimationTagsInsideDocumentBounds()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.InsertFrame(2, false);
            document.AddAnimationTag("Action", 1, 2);

            document.InsertFrame(1, false);

            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(2));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(3));

            document.RemoveFrame(2);

            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(2));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(2));
            Assert.That(document.AnimationTags[0].ToFrame, Is.LessThan(document.Frames.Count));
        }

        [Test]
        public void FrameCommandUndoRedo_RestoresAnimationTagRangesExactly()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.AddAnimationTag("One frame", 1, 1);
            SpriteCommandHistory history = new SpriteCommandHistory();

            history.Execute(new RemoveFrameCommand(document, 1));
            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(0));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(0));

            history.Undo();
            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(1));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(1));

            history.Redo();
            Assert.That(document.AnimationTags[0].FromFrame, Is.EqualTo(0));
            Assert.That(document.AnimationTags[0].ToFrame, Is.EqualTo(0));
        }

        [Test]
        public void Session_OwnsSelectionToolSettingsAndCommandHistoryWithoutEditorWindow()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.AddLayerTrack("Top");
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            session.SelectCell(1, 1);
            session.ActiveTool = SpriteToolboxTool.Eraser;
            session.BrushSize = 4;
            session.HorizontalSymmetry = true;
            SpriteCel cel = document.GetFrame(session.SelectedFrameIndex).GetCel(session.SelectedLayerIndex);
            session.Execute(new PixelChangesCommand(cel, new List<PixelChange> { new PixelChange(Vector2Int.zero, Color.clear, Color.red) }));

            Assert.That(session.SelectedFrameIndex, Is.EqualTo(1));
            Assert.That(session.SelectedLayerIndex, Is.EqualTo(1));
            Assert.That(session.ActiveTool, Is.EqualTo(SpriteToolboxTool.Eraser));
            Assert.That(session.BrushSize, Is.EqualTo(4));
            Assert.That(session.HorizontalSymmetry, Is.True);
            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));

            session.Undo();
            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.clear));
        }

        [Test]
        public void Session_DocumentEditedIncludesExecuteUndoAndRedo()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(1, 1));
            int editNotificationCount = 0;
            session.DocumentEdited += () => editNotificationCount++;

            session.Paint(Vector2Int.zero, Color.red);
            session.Undo();
            session.Redo();

            Assert.That(editNotificationCount, Is.EqualTo(3));
        }

        [Test]
        public void Session_OwnsPixelSelectionAndCopiesClipboardPixels()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(4, 4));
            Color32[] pixels = { (Color32)Color.red, (Color32)Color.blue };
            SpriteSelectionClipboard clipboard = new SpriteSelectionClipboard(2, 1, pixels);
            pixels[0] = (Color32)Color.green;

            session.SetPixelSelectionRect(new RectInt(1, 2, 2, 1));
            session.SetPixelSelectionActive(true);
            session.SetSelectionClipboard(clipboard);

            Assert.That(session.HasPixelSelection, Is.True);
            Assert.That(session.SelectionRect, Is.EqualTo(new RectInt(1, 2, 2, 1)));
            Assert.That(session.SelectionClipboard.GetPixel(0, 0), Is.EqualTo((Color32)Color.red));

            session.ClearPixelSelection();
            Assert.That(session.HasPixelSelection, Is.False);
        }

        [Test]
        public void Session_InvertPixelSelectionSelectsTheCanvasComplement()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(3, 2));
            session.SetPixelSelectionRect(new RectInt(1, 0, 1, 2));
            session.SetPixelSelectionActive(true);

            Assert.That(session.InvertPixelSelection(), Is.True);
            Assert.That(session.HasPixelSelection, Is.True);
            Assert.That(session.IsMagicPixelSelection, Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[]
            {
                new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(2, 1)
            }));

            Assert.That(session.InvertPixelSelection(), Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[] { new Vector2Int(1, 0), new Vector2Int(1, 1) }));
        }

        [Test]
        public void Session_LayerPropertiesAndRecolorAreUndoable()
        {
            SpriteDocument document = new SpriteDocument(1, 1);
            document.GetFrame(0).GetCel(0).SetPixel(Vector2Int.zero, Color.red);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            Assert.That(session.SetLayerLock(0, true), Is.True);
            Assert.That(document.GetLayerTrack(0).IsLocked, Is.True);
            session.Undo();
            Assert.That(document.GetLayerTrack(0).IsLocked, Is.False);

            Assert.That(session.RecolorSelectedFrame(Color.red, Color.blue), Is.True);
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.blue));
            Assert.That(document.Palette, Does.Contain(Color.blue));
            session.Undo();
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));
        }

        [Test]
        public void Session_BuildPaletteFromCanvasUsesVisibleCompositeColorsAndIsUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 1);
            document.SetPalette(new[] { Color.black });
            document.GetFrame(0).GetCel(0).SetPixel(new Vector2Int(0, 0), Color.red);
            document.GetFrame(0).GetCel(0).SetPixel(new Vector2Int(1, 0), Color.blue);
            document.AddLayerTrack("Top");
            document.GetFrame(0).GetCel(1).SetPixel(new Vector2Int(1, 0), Color.green);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            Assert.That(session.BuildPaletteFromCanvas(), Is.True);
            Assert.That(document.Palette, Is.EqualTo(new[] { Color.red, Color.green }));

            session.Undo();
            Assert.That(document.Palette, Is.EqualTo(new[] { Color.black }));
        }

        [Test]
        public void Session_RecolorMappingsUseCanvasSourcesAndApplyAsOneUndoableCommand()
        {
            SpriteDocument document = new SpriteDocument(2, 1);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), Color.red);
            cel.SetPixel(new Vector2Int(1, 0), Color.blue);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            SpriteCanvasColorMap sourceMap = session.CreateCanvasColorMap();
            Assert.That(sourceMap.Colors, Has.Count.EqualTo(2));
            Assert.That(sourceMap.Colors, Does.Contain(Color.red));
            Assert.That(sourceMap.Colors, Does.Contain(Color.blue));
            List<SpriteRecolorMapping> mappings = new List<SpriteRecolorMapping>
            {
                new SpriteRecolorMapping(Color.red, Color.green),
                new SpriteRecolorMapping(Color.blue, Color.blue)
            };

            Assert.That(session.ApplyRecolorMappings(mappings, sourceMap), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.green));
            Assert.That(cel.GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.blue));

            session.Undo();
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
        }

        [Test]
        public void Recolor_ColorReductionCreatesTheRequestedNumberOfPaletteTargets()
        {
            SpriteDocument document = new SpriteDocument(4, 1);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), new Color32(250, 80, 120, 255));
            cel.SetPixel(new Vector2Int(1, 0), new Color32(240, 70, 115, 255));
            cel.SetPixel(new Vector2Int(2, 0), new Color32(70, 120, 245, 255));
            cel.SetPixel(new Vector2Int(3, 0), new Color32(60, 110, 235, 255));
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            SpriteCanvasColorMap sourceMap = session.CreateCanvasColorMap();
            IReadOnlyList<SpriteRecolorMapping> mappings = session.CreateColorReductionMappings(sourceMap, 2);
            HashSet<Color32> targets = new HashSet<Color32>();
            foreach (SpriteRecolorMapping mapping in mappings)
            {
                targets.Add(mapping.Target);
            }

            Assert.That(mappings.Count, Is.EqualTo(4));
            Assert.That(targets.Count, Is.EqualTo(2));
        }

        [Test]
        public void Session_FillLinePickerAndSelectionOperationsDoNotNeedEditorWindow()
        {
            SpriteDocument document = new SpriteDocument(3, 3);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), Color.red);

            Assert.That(session.FillSelectedCel(new Vector2Int(1, 0), Color.blue, false, false), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.blue));
            Assert.That(session.DrawLineOnSelectedCel(new Vector2Int(0, 1), new Vector2Int(2, 1), Color.green, false, false), Is.True);
            Assert.That(session.PickCompositeColor(new Vector2Int(1, 1)), Is.EqualTo(Color.green));

            session.SetPixelSelectionRect(new RectInt(0, 1, 1, 1));
            session.SetPixelSelectionActive(true);
            Assert.That(session.CopyPixelSelection(), Is.True);
            Assert.That(session.CutPixelSelection(), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(0, 1)), Is.EqualTo(Color.clear));
            Assert.That(session.PastePixelSelection(), Is.True);
            Assert.That(session.HasFloatingSelection, Is.True);
            Assert.That(session.CommitFloatingSelection(), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(1, 2)), Is.EqualTo(Color.green));
        }

        [Test]
        public void Session_AnimationTagOperationsAreUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            Assert.That(session.AddAnimationTag("Idle", 0, 1), Is.True);
            Assert.That(document.AnimationTags[0].Color, Is.EqualTo((Color)new Color32(251, 223, 228, 255)));
            Assert.That(session.AddAnimationTag("Walk", 0, 1), Is.True);
            Assert.That(document.AnimationTags[1].Color, Is.EqualTo((Color)new Color32(144, 183, 220, 255)));
            Assert.That(session.UpdateAnimationTag(0, "Walk", 1, 1, SpriteAnimationDirection.Reverse, Color.yellow), Is.True);
            Assert.That(document.AnimationTags[0].Name, Is.EqualTo("Walk"));

            session.Undo();
            Assert.That(document.AnimationTags[0].Name, Is.EqualTo("Idle"));
            session.Undo();
            Assert.That(document.AnimationTags.Count, Is.EqualTo(1));
            session.Undo();
            Assert.That(document.AnimationTags, Is.Empty);
        }

        [Test]
        public void OperationResultAndServices_ReportChangedPixelsAndAdvancePlayback()
        {
            SpriteDocument document = new SpriteDocument(3, 2);
            document.InsertFrame(1, false);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            SpriteToolboxOperationResult fillResult = session.FillSelectedCelResult(Vector2Int.zero, Color.blue, false, false);
            Assert.That(fillResult.DidChange, Is.True);
            Assert.That(fillResult.PixelChanges.Count, Is.EqualTo(6));
            Assert.That(fillResult.InvalidatedBounds, Is.EqualTo(new RectInt(0, 0, 3, 2)));

            SpriteCel cel = document.GetFrame(0).GetCel(0);
            session.SetPrimaryColor(Color.red);
            session.BeginStroke(Vector2Int.zero);
            SpriteToolboxOperationResult strokeResult = session.CommitStroke();
            Assert.That(strokeResult.DidChange, Is.True);
            Assert.That(strokeResult.InvalidatedBounds, Is.EqualTo(new RectInt(0, 0, 1, 1)));

            int frameIndex = 0;
            session.Playback.IsPlaying = true;
            Assert.That(session.Playback.TryAdvance(document, ref frameIndex, 0d, null), Is.True);
            Assert.That(frameIndex, Is.EqualTo(1));
        }

        [Test]
        public void BrushStroke_RemovesNetZeroPixelChanges()
        {
            SpriteCel cel = new SpriteDocument(1, 1).GetFrame(0).GetCel(0);
            BrushStrokeService stroke = new BrushStrokeService();
            stroke.Begin();
            stroke.Record(cel, Vector2Int.zero, Color.red);
            cel.SetPixel(Vector2Int.zero, Color.red);
            stroke.Record(cel, Vector2Int.zero, Color.clear);

            SpriteToolboxOperationResult result = stroke.Commit();
            Assert.That(result.DidChange, Is.False);
            Assert.That(result.PixelChanges, Is.Empty);
        }

        [Test]
        public void Session_StrokeCanUseExplicitSecondaryColorAndRemainsUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 1);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SetPrimaryColor(Color.red);
            session.SetSecondaryColor(Color.blue);

            session.BeginStroke(new Vector2Int(0, 0), session.SecondaryColor);
            session.ContinueStroke(new Vector2Int(1, 0), session.SecondaryColor);
            SpriteToolboxOperationResult result = session.CommitStroke();

            Assert.That(result.DidChange, Is.True);
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.blue));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.blue));

            session.Undo();
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.clear));
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.clear));
        }

        [Test]
        public void Session_OrchestratesPixelPerfectStrokeAndCommitsOneUndoableEdit()
        {
            SpriteDocument document = new SpriteDocument(3, 3);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SetPrimaryColor(Color.red);
            session.SetPixelPerfectPencil(true);
            int editNotificationCount = 0;
            session.DocumentEdited += () => editNotificationCount++;

            session.BeginStroke(new Vector2Int(0, 0));
            session.ContinueStroke(new Vector2Int(1, 0));
            session.ContinueStroke(new Vector2Int(1, 1));
            SpriteToolboxOperationResult result = session.CommitStroke();

            SpriteCel cel = document.GetFrame(0).GetCel(0);
            Assert.That(result.DidChange, Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.clear));
            Assert.That(cel.GetPixel(new Vector2Int(1, 1)), Is.EqualTo(Color.red));
            Assert.That(editNotificationCount, Is.EqualTo(1));

            session.Undo();
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.clear));
            Assert.That(cel.GetPixel(new Vector2Int(1, 1)), Is.EqualTo(Color.clear));
        }

        [Test]
        public void AnimationPlayback_RespectsReverseAndPingPongTagDirections()
        {
            SpriteDocument document = new SpriteDocument(1, 1);
            document.InsertFrame(1, false);
            document.InsertFrame(2, false);
            AnimationPlaybackService playback = new AnimationPlaybackService { IsPlaying = true };
            SpriteAnimationTag reverseTag = new SpriteAnimationTag("Reverse", 0, 2) { Direction = SpriteAnimationDirection.Reverse };
            int reverseFrame = 1;
            Assert.That(playback.TryAdvance(document, ref reverseFrame, 0d, reverseTag), Is.True);
            Assert.That(reverseFrame, Is.EqualTo(0));

            playback.ResetSchedule();
            SpriteAnimationTag pingPongTag = new SpriteAnimationTag("PingPong", 0, 2) { Direction = SpriteAnimationDirection.PingPong };
            int pingPongFrame = 1;
            Assert.That(playback.TryAdvance(document, ref pingPongFrame, 1d, pingPongTag), Is.True);
            Assert.That(pingPongFrame, Is.EqualTo(2));
            Assert.That(playback.TryAdvance(document, ref pingPongFrame, 2d, pingPongTag), Is.True);
            Assert.That(pingPongFrame, Is.EqualTo(1));
        }

        [Test]
        public void Line_ClipsSegmentsEnteringAndLeavingTheCanvasInBothDirections()
        {
            SpriteDocument document = new SpriteDocument(3, 3);
            SpriteToolboxSession session = new SpriteToolboxSession(document);

            SpriteToolboxOperationResult forward = session.DrawLineOnSelectedCelResult(new Vector2Int(-2, 1), new Vector2Int(4, 1), Color.red, false, false);

            Assert.That(forward.PixelChanges.Count, Is.EqualTo(3));
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            Assert.That(cel.GetPixel(new Vector2Int(0, 1)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.red));

            session.Undo();
            SpriteToolboxOperationResult reverse = session.DrawLineOnSelectedCelResult(new Vector2Int(4, 1), new Vector2Int(-2, 1), Color.blue, false, false);

            Assert.That(reverse.PixelChanges.Count, Is.EqualTo(3));
            Assert.That(cel.GetPixel(new Vector2Int(0, 1)), Is.EqualTo(Color.blue));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.blue));
        }

        [Test]
        public void SelectionMove_UpdatesBoundsPixelsAndUndoAsOneOperation()
        {
            SpriteDocument document = new SpriteDocument(4, 3);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), Color.red);
            cel.SetPixel(new Vector2Int(1, 0), Color.blue);
            session.SetPixelSelectionRect(new RectInt(0, 0, 2, 1));
            session.SetPixelSelectionActive(true);
            Assert.That(session.BeginFloatingMoveSelection(), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(session.NudgeFloatingSelection(Vector2Int.right), Is.True);
            Assert.That(session.SelectionRect, Is.EqualTo(new RectInt(1, 0, 2, 1)));
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(session.MoveFloatingSelection(new Vector2Int(2, 1)), Is.True);
            Assert.That(session.SelectionRect, Is.EqualTo(new RectInt(2, 1, 2, 1)));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.clear));
            Assert.That(session.CommitFloatingSelection(), Is.True);
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.clear));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(3, 1)), Is.EqualTo(Color.blue));

            session.Undo();
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.blue));
            Assert.That(cel.GetPixel(new Vector2Int(2, 1)), Is.EqualTo(Color.clear));
        }

        [Test]
        public void FloatingSelection_CancelRestoresMagicSelectionWithoutChangingPixels()
        {
            SpriteDocument document = new SpriteDocument(3, 1);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(Vector2Int.zero, Color.red);
            cel.SetPixel(new Vector2Int(2, 0), Color.blue);
            session.SetMagicPixelSelection(Vector2Int.zero);

            Assert.That(session.BeginFloatingMoveSelection(), Is.True);
            Assert.That(session.MoveFloatingSelection(new Vector2Int(1, 0)), Is.True);
            Assert.That(session.CancelFloatingSelection(), Is.True);

            Assert.That(session.IsMagicPixelSelection, Is.True);
            Assert.That(session.PixelSelectionPositions, Is.EquivalentTo(new[] { Vector2Int.zero }));
            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));
            Assert.That(cel.GetPixel(new Vector2Int(2, 0)), Is.EqualTo(Color.blue));
        }

        [Test]
        public void FloatingPaste_TransparentPixelsDoNotOverwriteDestination()
        {
            SpriteDocument document = new SpriteDocument(2, 1);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(Vector2Int.zero, Color.red);
            cel.SetPixel(Vector2Int.right, Color.blue);
            session.SetSelectionClipboard(new SpriteSelectionClipboard(2, 1, new[] { (Color32)Color.green, (Color32)Color.clear }));

            Assert.That(session.PastePixelSelection(), Is.True);
            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.red));
            Assert.That(session.CommitFloatingSelection(), Is.True);

            Assert.That(cel.GetPixel(Vector2Int.zero), Is.EqualTo(Color.green));
            Assert.That(cel.GetPixel(Vector2Int.right), Is.EqualTo(Color.blue));
        }

        [Test]
        public void RotSpriteRotation_ZeroDegreesPreservesPixelsAndBounds()
        {
            Color32[] sourcePixels = { (Color32)Color.red, (Color32)Color.blue };

            SpriteRotatedPixels result = SpriteRotSpriteRotationService.Rotate(sourcePixels, 2, 1, 0f);

            Assert.That(result.Bounds, Is.EqualTo(new RectInt(0, 0, 2, 1)));
            Assert.That(result.Pixels, Is.EqualTo(sourcePixels));
        }

        [Test]
        public void Stroke_TiledModeWrapsOutsideCoordinates()
        {
            SpriteDocument document = new SpriteDocument(3, 2);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SetPrimaryColor(Color.green);
            session.SetTiledMode(true);

            session.BeginStroke(new Vector2Int(-1, 0));
            SpriteToolboxOperationResult result = session.CommitStroke();

            Assert.That(result.DidChange, Is.True);
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(2, 0)), Is.EqualTo(Color.green));
        }

        [Test]
        public void Stroke_SymmetryMirrorsAcrossBothAxes()
        {
            SpriteDocument document = new SpriteDocument(3, 3);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SetPrimaryColor(Color.yellow);
            session.SetHorizontalSymmetry(true);
            session.SetVerticalSymmetry(true);

            session.BeginStroke(Vector2Int.zero);
            SpriteToolboxOperationResult result = session.CommitStroke();

            Assert.That(result.PixelChanges.Count, Is.EqualTo(4));
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            Assert.That(cel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.yellow));
            Assert.That(cel.GetPixel(new Vector2Int(2, 0)), Is.EqualTo(Color.yellow));
            Assert.That(cel.GetPixel(new Vector2Int(0, 2)), Is.EqualTo(Color.yellow));
            Assert.That(cel.GetPixel(new Vector2Int(2, 2)), Is.EqualTo(Color.yellow));
        }

        [Test]
        public void OnionCache_IsInvalidatedWhenAnAdjacentFrameChanges()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.InsertFrame(2, false);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            CanvasRenderCache cache = new CanvasRenderCache();
            session.DocumentChanged += cache.InvalidateOnionSkin;

            try
            {
                cache.PrepareOnionSkin(document, 1, false);
                Assert.That(cache.HasCachedOnionSkin, Is.True);

                session.Paint(Vector2Int.zero, Color.magenta);

                Assert.That(cache.HasCachedOnionSkin, Is.False);
            }
            finally
            {
                session.DocumentChanged -= cache.InvalidateOnionSkin;
                cache.Dispose();
            }
        }

        [Test]
        public void Session_CachesPresentationStatesUntilStructureChanges()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(2, 2));
            IReadOnlyList<SpriteToolboxLayerState> initialLayers = session.Layers;
            IReadOnlyList<SpriteToolboxFrameState> initialFrames = session.Frames;

            Assert.That(session.Layers, Is.SameAs(initialLayers));
            Assert.That(session.Frames, Is.SameAs(initialFrames));

            session.Paint(Vector2Int.zero, Color.red);
            Assert.That(session.Layers, Is.SameAs(initialLayers));
            Assert.That(session.Frames, Is.SameAs(initialFrames));

            session.AddLayer("Second");
            Assert.That(session.Layers, Is.Not.SameAs(initialLayers));
            Assert.That(session.Frames, Is.Not.SameAs(initialFrames));
        }

        [Test]
        public void Session_DuplicateLayer_CopiesPixelsAndIsUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.AddLayerTrack("Ink");
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            session.SelectCell(0, 1);
            session.Paint(Vector2Int.zero, Color.magenta);

            bool duplicated = session.DuplicateLayer();

            Assert.That(duplicated, Is.True);
            Assert.That(document.LayerTracks.Count, Is.EqualTo(3));
            Assert.That(document.GetLayerTrack(2).Name, Is.EqualTo("Ink Copy"));
            Assert.That(session.SelectedLayerIndex, Is.EqualTo(2));
            Assert.That(document.GetFrame(0).GetCel(2).GetPixel(Vector2Int.zero), Is.EqualTo(Color.magenta));

            session.Undo();

            Assert.That(document.LayerTracks.Count, Is.EqualTo(2));
            Assert.That(document.GetFrame(0).GetCel(1).GetPixel(Vector2Int.zero), Is.EqualTo(Color.magenta));
        }

        [Test]
        public void Session_ReportsPixelBoundsAndStructuralChangesSeparately()
        {
            SpriteToolboxSession session = new SpriteToolboxSession(new SpriteDocument(3, 3));
            SpriteToolboxDocumentChange lastChange = SpriteToolboxDocumentChange.Structure();
            session.DocumentChangedDetailed += change => lastChange = change;

            session.Paint(new Vector2Int(1, 2), Color.red);

            Assert.That(lastChange.Kind, Is.EqualTo(SpriteToolboxDocumentChangeKind.Pixels));
            Assert.That(lastChange.FrameIndex, Is.EqualTo(0));
            Assert.That(lastChange.InvalidatedBounds, Is.EqualTo(new RectInt(1, 2, 1, 1)));

            session.AddFrame(false);
            Assert.That(lastChange.Kind, Is.EqualTo(SpriteToolboxDocumentChangeKind.Structure));
            Assert.That(lastChange.IsStructural, Is.True);
        }

        [Test]
        public void OnionCache_ReusesTexturesAfterInvalidation()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.InsertFrame(1, false);
            document.InsertFrame(2, false);
            CanvasRenderCache cache = new CanvasRenderCache();

            try
            {
                cache.PrepareOnionSkin(document, 1, false);
                int previousTextureId = cache.PreviousOnionTextureInstanceId;
                int nextTextureId = cache.NextOnionTextureInstanceId;

                cache.InvalidateOnionSkinFrame(document, 0);
                cache.PrepareOnionSkin(document, 1, false);

                Assert.That(cache.PreviousOnionTextureInstanceId, Is.EqualTo(previousTextureId));
                Assert.That(cache.NextOnionTextureInstanceId, Is.EqualTo(nextTextureId));
            }
            finally
            {
                cache.Dispose();
            }
        }

        [Test]
        public void DocumentMemoryEstimate_UsesFourBytesPerStoredPixel()
        {
            SpriteDocument onePixel = new SpriteDocument(1, 1);
            SpriteDocument twoPixels = new SpriteDocument(2, 1);

            Assert.That(twoPixels.EstimateMemoryBytes() - onePixel.EstimateMemoryBytes(), Is.EqualTo(4L));
        }

        [Test]
        public void Session_ResizeCanvasPreservesTopLeftPixelsAndIsUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            cel.SetPixel(new Vector2Int(0, 0), Color.red);
            cel.SetPixel(new Vector2Int(1, 1), Color.blue);

            Assert.That(session.ResizeCanvas(3, 1), Is.True);
            SpriteCel resizedCel = document.GetFrame(0).GetCel(0);
            Assert.That(document.Width, Is.EqualTo(3));
            Assert.That(document.Height, Is.EqualTo(1));
            Assert.That(resizedCel.GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(resizedCel.GetPixel(new Vector2Int(2, 0)), Is.EqualTo(Color.clear));

            session.Undo();
            SpriteCel restoredCel = document.GetFrame(0).GetCel(0);
            Assert.That(document.Width, Is.EqualTo(2));
            Assert.That(document.Height, Is.EqualTo(2));
            Assert.That(restoredCel.GetPixel(new Vector2Int(1, 1)), Is.EqualTo(Color.blue));
        }

        [Test]
        public void Session_FlipCanvasTransformsEveryCelAndIsUndoable()
        {
            SpriteDocument document = new SpriteDocument(2, 2);
            document.AddLayerTrack("Layer 2");
            document.AddFrame();
            SpriteToolboxSession session = new SpriteToolboxSession(document);
            document.GetFrame(0).GetCel(0).SetPixel(new Vector2Int(0, 0), Color.red);
            document.GetFrame(1).GetCel(1).SetPixel(new Vector2Int(1, 1), Color.blue);

            Assert.That(session.FlipCanvas(true), Is.True);
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.red));
            Assert.That(document.GetFrame(1).GetCel(1).GetPixel(new Vector2Int(0, 1)), Is.EqualTo(Color.blue));

            session.Undo();
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(0, 0)), Is.EqualTo(Color.red));
            Assert.That(document.GetFrame(1).GetCel(1).GetPixel(new Vector2Int(1, 1)), Is.EqualTo(Color.blue));

            Assert.That(session.FlipCanvas(false), Is.True);
            Assert.That(document.GetFrame(0).GetCel(0).GetPixel(new Vector2Int(0, 1)), Is.EqualTo(Color.red));
            Assert.That(document.GetFrame(1).GetCel(1).GetPixel(new Vector2Int(1, 0)), Is.EqualTo(Color.blue));
        }

        [Test]
        public void Panels_ConsumeStateAndActionContractsWithoutEditorWindow()
        {
            FakeState state = new FakeState();
            FakePanelActions actions = new FakePanelActions();
            FakeOnionSkinSettings onionSkinSettings = new FakeOnionSkinSettings();
            int drawCount = 0;
            SpriteToolboxTimelinePanel timeline = new SpriteToolboxTimelinePanel(state, actions, onionSkinSettings, (receivedState, receivedActions, receivedSettings) =>
            {
                if (ReferenceEquals(state, receivedState) && ReferenceEquals(actions, receivedActions) && ReferenceEquals(onionSkinSettings, receivedSettings)) drawCount++;
            });
            SpriteToolboxToolsPanel tools = new SpriteToolboxToolsPanel(state, actions, (receivedState, receivedActions) =>
            {
                if (ReferenceEquals(state, receivedState) && ReferenceEquals(actions, receivedActions)) drawCount++;
            });
            SpriteToolboxCanvasPanel canvas = new SpriteToolboxCanvasPanel(state, actions, (receivedState, receivedActions) =>
            {
                if (ReferenceEquals(state, receivedState) && ReferenceEquals(actions, receivedActions)) drawCount++;
            });

            timeline.Draw();
            tools.Draw();
            canvas.Draw();

            Assert.That(drawCount, Is.EqualTo(3));
        }

        [Test]
        public void ReferenceImport_ReducesColorsWithoutMappingToActivePaletteAndCanvasSize()
        {
            Color32[] sourcePixels =
            {
                new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255), new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255),
                new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255), new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255),
                new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255), new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255),
                new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255), new Color32(240, 20, 20, 255), new Color32(20, 20, 240, 255)
            };
            List<Color> palette = new List<Color> { Color.red, Color.blue };

            Color32[] importedPixels = SpriteReferenceImageImportService.ReduceAndResize(sourcePixels, 4, 4, palette.Count, 2, 2);

            Assert.That(importedPixels.Length, Is.EqualTo(4));
            Assert.That(importedPixels[0].a, Is.EqualTo(255));
            Assert.That(importedPixels[0].Equals((Color32)Color.red) || importedPixels[0].Equals((Color32)Color.blue), Is.False);
        }

        [Test]
        public void LightPadLayout_MatchHeightCentersReferenceAndPreservesAspectRatio()
        {
            Rect canvasRect = new Rect(10f, 20f, 200f, 100f);
            Rect destinationRect = SpriteToolboxLightPadLayout.CalculateDestinationRect(canvasRect, 400, 100, SpriteLightPadAlignment.MatchHeight, 8f);

            Assert.That(destinationRect.height, Is.EqualTo(100f));
            Assert.That(destinationRect.width, Is.EqualTo(400f));
            Assert.That(destinationRect.center, Is.EqualTo(canvasRect.center));
        }

        [Test]
        public void LightPadLayout_ZoomLevelScalesFromReferenceFit()
        {
            Rect canvasRect = new Rect(0f, 0f, 160f, 160f);
            Rect destinationRect = SpriteToolboxLightPadLayout.CalculateDestinationRect(canvasRect, 12, 9, SpriteLightPadAlignment.ZoomLevel, 1f);

            Assert.That(destinationRect.size, Is.EqualTo(new Vector2(160f, 120f)));
            Assert.That(destinationRect.center, Is.EqualTo(canvasRect.center));
        }

        [Test]
        public void LightPadController_MissingTextureClearsSnapshotWithoutDocumentCommands()
        {
            SpriteToolboxLightPadSettings settings = new SpriteToolboxLightPadSettings
            {
                IsEnabled = true,
                SourceType = SpriteLightPadSourceType.Texture
            };
            SpriteToolboxLightPadController controller = new SpriteToolboxLightPadController(settings, new SpriteToolboxLightPadEditorState());

            controller.Refresh();

            Assert.That(controller.ReferenceTexture, Is.Null);
            Assert.That(controller.Status, Does.Contain("needs a Texture2D or RenderTexture"));
            controller.Dispose();
        }

        [Test]
        public void LightPadController_UsesTextureSourceDirectly()
        {
            Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            SpriteToolboxLightPadSettings settings = new SpriteToolboxLightPadSettings
            {
                IsEnabled = true,
                SourceType = SpriteLightPadSourceType.Texture,
                SourceTexture = sourceTexture
            };
            SpriteToolboxLightPadController controller = new SpriteToolboxLightPadController(settings, new SpriteToolboxLightPadEditorState());

            controller.Refresh();

            Assert.That(controller.ReferenceTexture, Is.SameAs(sourceTexture));
            Assert.That(controller.Status, Is.EqualTo("Texture reference is used directly."));
            controller.Dispose();
            UnityEngine.Object.DestroyImmediate(sourceTexture);
        }

        [Test]
        public void LightPadController_MovesTextureReferenceButNotSceneViewReference()
        {
            SpriteToolboxLightPadSettings textureSettings = new SpriteToolboxLightPadSettings
            {
                SourceType = SpriteLightPadSourceType.Texture
            };
            SpriteToolboxLightPadController textureController = new SpriteToolboxLightPadController(textureSettings, new SpriteToolboxLightPadEditorState());
            textureController.MoveReference(new Vector2(2f, -3f));

            Assert.That(textureSettings.PositionOffset, Is.EqualTo(new Vector2(2f, -3f)));
            textureController.Dispose();

            SpriteToolboxLightPadSettings sceneViewSettings = new SpriteToolboxLightPadSettings
            {
                SourceType = SpriteLightPadSourceType.SceneView
            };
            SpriteToolboxLightPadController sceneViewController = new SpriteToolboxLightPadController(sceneViewSettings, new SpriteToolboxLightPadEditorState());
            sceneViewController.MoveReference(new Vector2(2f, -3f));

            Assert.That(sceneViewSettings.PositionOffset, Is.EqualTo(Vector2.zero));
            sceneViewController.Dispose();
        }

        [Test]
        public void PalettePanel_CanUseFakeStateAndActionsWithoutEditorWindow()
        {
            FakeState state = new FakeState();
            FakePaletteActions actions = new FakePaletteActions();
            bool wasDrawn = false;
            SpriteToolboxPalettePanel panel = new SpriteToolboxPalettePanel(state, actions, (receivedState, receivedActions) =>
            {
                wasDrawn = ReferenceEquals(state, receivedState) && ReferenceEquals(actions, receivedActions);
            });

            panel.Draw();

            Assert.That(wasDrawn, Is.True);
        }

        private sealed class FakeState : ISpriteToolboxState
        {
            private static readonly IReadOnlyList<Color> Colors = new List<Color> { Color.black, Color.white };
            private static readonly IReadOnlyList<SpriteToolboxLayerState> LayersState = new List<SpriteToolboxLayerState> { new SpriteToolboxLayerState("Layer 1", true, false, 1f) };
            private static readonly IReadOnlyList<SpriteToolboxFrameState> FramesState = new List<SpriteToolboxFrameState> { new SpriteToolboxFrameState(0.1f) };
            private static readonly IReadOnlyList<SpriteToolboxAnimationTagState> TagsState = new List<SpriteToolboxAnimationTagState>();

            public int DocumentWidth => 1;
            public int DocumentHeight => 1;
            public int SelectedFrameIndex => 0;
            public int SelectedLayerIndex => 0;
            public int ActiveAnimationTagIndex => -1;
            public bool IsPreviewPlaying => false;
            public SpriteToolboxTool ActiveTool => SpriteToolboxTool.Pencil;
            public int BrushSize => 1;
            public SpriteBrushShape BrushShape => SpriteBrushShape.Square;
            public int FillTolerance => 0;
            public SpriteSelectionMode SelectionMode => SpriteSelectionMode.Rectangular;
            public bool HorizontalSymmetry => false;
            public bool VerticalSymmetry => false;
            public bool TiledMode => false;
            public bool PixelPerfectPencil => false;
            public Color PrimaryColor => Color.black;
            public Color SecondaryColor => Color.white;
            public IReadOnlyList<Color> Palette => Colors;
            public IReadOnlyList<SpriteToolboxLayerState> Layers => LayersState;
            public IReadOnlyList<SpriteToolboxFrameState> Frames => FramesState;
            public IReadOnlyList<SpriteToolboxAnimationTagState> AnimationTags => TagsState;
            public bool CelHasVisiblePixels(int frameIndex, int layerIndex) { return false; }
            public RectInt SelectionRect => new RectInt();
            public bool HasPixelSelection => false;
            public bool HasSelectionClipboard => false;
        }

        private sealed class FakePaletteActions : IPaletteActions
        {
            public void SetPrimaryColor(Color color) { }
            public void SetSecondaryColor(Color color) { }
            public void SwapColors() { }
            public bool AddPaletteColor(Color color) { return false; }
            public bool SetPalette(IReadOnlyList<Color> colors) { return false; }
            public bool SortPaletteByColorFamilies() { return false; }
            public bool BuildPaletteFromCanvas() { return false; }
            public bool RecolorSelectedFrame(Color sourceColor, Color targetColor) { return false; }
        }

        private sealed class FakeOnionSkinSettings : IOnionSkinSettings
        {
            public bool ShowPreviousOnionSkin { get; set; }
            public bool ShowNextOnionSkin { get; set; }
            public bool LoopOnionSkin { get; set; }
        }

        private sealed class FakePanelActions : ITimelineActions, IToolActions
        {
            public void SelectFrame(int frameIndex) { }
            public void SelectLayer(int layerIndex) { }
            public void SelectCell(int frameIndex, int layerIndex) { }
            public bool AddFrame(bool duplicateCurrentFrame) { return false; }
            public bool RemoveFrame() { return false; }
            public bool MoveFrame(int direction) { return false; }
            public bool MoveFrameTo(int sourceIndex, int destinationIndex) { return false; }
            public bool AddLayer(string name) { return false; }
            public bool DuplicateLayer() { return false; }
            public bool RemoveLayer() { return false; }
            public bool MoveLayer(int direction) { return false; }
            public bool MoveLayerTo(int sourceIndex, int destinationIndex) { return false; }
            public bool MergeLayerDown() { return false; }
            public bool SetFrameDuration(float duration) { return false; }
            public bool ClearSelectedCel() { return false; }
            public bool SetLayerVisibility(int layerIndex, bool isVisible) { return false; }
            public bool SetLayerLock(int layerIndex, bool isLocked) { return false; }
            public bool SetLayerName(int layerIndex, string name) { return false; }
            public bool SetAllLayersVisibility(bool isVisible) { return false; }
            public bool SetAllLayersLock(bool isLocked) { return false; }
            public bool AddAnimationTag(string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color) { return false; }
            public bool UpdateAnimationTag(int tagIndex, string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color) { return false; }
            public bool RemoveAnimationTag(int tagIndex) { return false; }
            public void SelectAnimationTag(int tagIndex) { }
            public void SetPreviewPlaying(bool value) { }
            public void SetActiveTool(SpriteToolboxTool tool) { }
            public void SetBrushSize(int size) { }
            public void SetBrushShape(SpriteBrushShape shape) { }
            public void SetFillTolerance(int tolerance) { }
            public void SetSelectionMode(SpriteSelectionMode mode) { }
            public void SetHorizontalSymmetry(bool value) { }
            public void SetVerticalSymmetry(bool value) { }
            public void SetTiledMode(bool value) { }
            public void SetPixelPerfectPencil(bool value) { }
            public bool CropCanvasToSelection() { return false; }
            public bool CopyPixelSelection() { return false; }
            public bool CutPixelSelection() { return false; }
            public bool PastePixelSelection() { return false; }
            public bool BeginFloatingMoveSelection() { return false; }
            public bool MoveFloatingSelection(Vector2Int requestedDelta) { return false; }
            public bool CommitFloatingSelection() { return false; }
            public bool CancelFloatingSelection() { return false; }
            public bool FillPixelSelection(Color color) { return false; }
            public bool ClearPixelSelectionPixels() { return false; }
            public bool FlipPixelSelection(bool horizontally) { return false; }
            public bool InvertPixelSelection() { return false; }
            public bool RecolorSelectedFrame(Color sourceColor, Color targetColor) { return false; }
        }
    }
}
