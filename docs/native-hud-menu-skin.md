# Native HUD menu and help skins

The Community panel and bottom-button help use the existing Data_Broyal `Interface/GFx/main_IE.ozd` atlas. No proprietary assets are added.

The source was verified against the decrypted Scaleform `main.ozg` definitions:

- `SubMenuScrollingList`: sprite 18, shape 17; native bounds 145 x 83. Subimages 16 (top, 0/229/145/34), 15 (middle, 299/163/145/8), and 14 (bottom, 152/163/145/9). The middle is vertically stretched; top and bottom keep their heights.
- `LRSubMenu`: sprite 37; 18-pixel rows, 12-pixel font. Key color RGB 254/42/42, normal name 145/145/145, hovered name 255/0/0. Keys and names are independent fields, not a combined centered string.
- `TooltipMainButton`: sprite 199, shape 195; 28-pixel height. Subimages 192 (left, 211/229/26/28), 194 (middle, 1016/0/8/28), and 193 (right, 183/229/26/28). Key is red, description white. The center stretches to fit the text.

Android/iOS retain larger touch rows; desktop uses the native compact rows. The functional menu remains Amigos, Guild, Party.

Validation: parsed the original GFx shape/subimage and text tags, reconstructed a visual preview from the atlas, and parsed the changed C# files with Roslyn without compilation. Runtime checks remain: hover every bottom button, open Community at different resolutions, select all three destinations, and test touch selection and dismissal.
