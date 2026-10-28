精神科药物游戏道具：横版药盒 + 药瓶

两个独立模型：
Medication_Box_Horizontal：11.5 × 6.5 × 3 cm（宽、高、深），192 个三角面。
Medication_Bottle：高 9.2 cm，直径约 4.59 cm，5952 个三角面。瓶身、标签、瓶颈环、瓶盖分别为独立网格。
全部以米为单位，Y 轴向上，原点在底面中心。
模型是静态封闭道具，不含药片、内部结构、开盖动画。
包装是虚构抗抑郁类处方药，写有 Rx / ANTIDEPRESSANT / TABLETS / PRESCRIPTION ONLY。

文件
每个模型提供 OBJ 和内嵌贴图的 GLB。
Medication_Materials.mtl 是 OBJ 材质文件，请保留与模型的相对位置。
Textures 内有四套材质：Carton_Print（盒子正反面）、Carton_Side（侧面和顶底）、Bottle_Label（环绕标签）、White_Plastic（瓶身、瓶颈和瓶盖）。
每套提供 BaseColor、Normal、Roughness、MetallicSmoothness 和 ORM，共 20 张 PNG。
纸盒/标签为哑光，塑料瓶身有适度光泽；瓶盖防滑条为真实网格细节。
Actual_Model_Preview.png 是根据交付网格和贴图生成的实际预览。
Source/build_props.py 为可再生成模型和贴图的源代码，依赖 Python、numpy、scipy、Pillow 以及 DejaVu Sans 字体。

Unity 导入
1. 解压 ZIP，把整个 Psych_Medication_Props 文件夹放进 Assets。
2. 等待导入和脚本编译结束。
3. 点击 Tools > Eco > Create Medication Prop Prefabs。
4. 在新生成的 Prefabs 文件夹里找到药盒和药瓶，拖进场景。
模型是实际大小的小物件，选中后按 F 聚焦查看。需要放大时统一调整 Scale。
自动配置优先使用 URP/Lit，无 URP 时使用 Standard；HDRP 需要手动配置。
脚本会建立材质与简化碰撞体，不添加 Rigidbody，不修改现有场景。

手动贴图配置
BaseColor → Base Map；颜色设为白色。
Normal → Normal Map；贴图类型设为 Normal map，强度可用 0.4。
MetallicSmoothness → Metallic Map；关闭 sRGB，Smoothness 来源选 Metallic Alpha。
Roughness 用于通用 PBR 工作流；不要把 Roughness 当成 Smoothness 直接使用。
ORM 的 R=遮蔽（全白）、G=粗糙度、B=金属度（全黑）；用于 glTF，Unity URP 不直接使用这个通道组合。
GLB 已内嵌颜色、法线和金属粗糙度纹理。Unity 默认不直接导入 GLB，请使用上述脚本或 OBJ。

验证
已检查网格索引、法线、UV、尺寸、GLB 结构及贴图内嵌，并检查实际渲染和 ZIP 完整解压。
当前环境没有 Unity 编辑器，因此配置脚本未在 Unity 内编译或运行验证。
