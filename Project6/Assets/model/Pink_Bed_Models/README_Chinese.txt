淡粉色床模型 — 整齐版 / 凌乱版

文件
Bed_Neat.obj：整齐版，可直接导入 Unity。
Bed_Messy.obj：凌乱版，可直接导入 Unity。
Pink_Bed_Materials.mtl：OBJ 材质引用，请与 OBJ 放在同一文件夹。
Textures：4 套 1024 × 1024 PNG 贴图（淡粉布料、米白布料、木材）。
Bed_Neat.glb / Bed_Messy.glb：嵌入颜色、法线贴图，可在支持 glTF 的软件中打开、旋转查看；Unity 默认不直接支持 GLB，Unity 请使用 OBJ。
Bed_*_Preview.png：根据交付模型网格渲染的实际预览。
Editor/PinkBedSetup.cs：Unity 编辑器辅助脚本，可生成带材质和简单碰撞体的 Prefab。
Source/build_beds.py：模型生成源代码，依赖 Python + numpy + scipy + Pillow。

Unity 导入
1. 解压 ZIP，把整个 Pink_Bed_Models 文件夹放到项目的 Assets 里。
2. 等待 Unity 完成导入和脚本编译。
3. 菜单选择 Tools > Eco > Create Pink Bed Prefabs。
4. 在新生成的 Prefabs 文件夹里，把 Bed_Neat 或 Bed_Messy 拖进场景。
脚本优先使用 URP/Lit；项目没有 URP 时使用 Standard。HDRP 项目需自行设置材质。
脚本只在编辑器运行，不会加入游戏运行时逻辑。

手动设置材质（不使用辅助脚本时）
创建四个 URP/Lit 材质，分别对应 BlushFabric、BlushQuilt、IvoryFabric、WarmWood。
*_BaseColor.png → Base Map，材质颜色保持白色。
*_Normal.png → Normal Map；在图片 Inspector 中 Texture Type 选择 Normal map，强度建议 0.4。
*_MetallicSmoothness.png → Metallic Map；sRGB 关闭，Smoothness 来源选择 Metallic Alpha。
*_Roughness.png 是通用粗糙度贴图，不是 URP 的 Smoothness 贴图；不要直接放进 Base Map。
木材使用 WarmWood；床垫、白枕头和白毯子使用 IvoryFabric；粉色被子使用 BlushQuilt，粉色枕头使用 BlushFabric。

模型设置
单位：米；Y 轴向上；原点在床的地面中心。
床垫约 1.80 × 2.00 米；含床头总宽约 2.10 米、长约 2.22 米、高约 1.50 米。
每版 30 个独立网格部件、81,744 个三角面（含布料厚度），四个枕头。
两版共用材质，但被子和枕头有不同的真实网格形态。
褶皱和绗缝起伏由几何实现；法线贴图提供细微织物表面纹理。
两版是静态场景道具，无骨骼、无动态布料模拟、无动画。
碰撞体是简化盒体，供静态摆放使用。

已验证：OBJ 索引、有限数值、UV、GLB 二进制布局及实际网格渲染。
当前环境没有 Unity 编辑器，因此未在 Unity 内编译或运行辅助脚本；最终灯光、颜色与阴影以项目中的渲染设置为准。
设计图是风格参考；本包为根据设计制作的简化游戏模型，不是图片的自动三维扫描。
