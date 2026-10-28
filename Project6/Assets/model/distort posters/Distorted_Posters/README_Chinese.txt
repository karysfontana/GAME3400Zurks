三张扭曲海报：模型 + 贴图

01 Poster_01_Riot_Distorted：清晰橙色 RIOT 标题，背景模糊、涂抹、黑色阴影。
02 Poster_02_Seventeen_Distorted：已选定的杂志涂抹、文字拖影版本。
03 Poster_03_PrettyLittleLiars_Distorted：最终确认的整张海报强波浪扭曲版。不是剪影版，也没有单独针对脸部的局部扭曲。

本包有三个独立 OBJ + MTL、三个内嵌贴图的 GLB，以及三张独立 PNG。
尺寸与正常版一致：宽 0.50 m × 高 0.65 m × 厚 0.0008 m。
原点在纸张中心，Y 轴向上，本地 +Z 为印刷面。每张 16 个三角面。
这是薄纸海报模型，扭曲效果在贴图上，纸张网格保持平整，方便在相同墙面位置替换。
RIOT 保留上下白边，另外两张留窄侧边。
Actual_Model_Preview.png 是本包模型和实际贴图的渲染预览。

Unity 使用
1. 解压 ZIP，将其中整个 Distorted_Posters 文件夹放进 Assets。
2. 如果之前已导入旧版 Distorted_Posters，请在原文件夹内更新同名文件，不要另建第二份文件夹，否则会有重复脚本类。Normal_Posters 正常版文件夹无需修改。
3. 等导入、编译结束，点击 Tools > Eco > Create Distorted Poster Prefabs。
4. 打开 Distorted_Posters/Prefabs，里面有三张独立 Prefab。
5. 在第二场景使用对应 Prefab，并复制正常版海报的 Position、Rotation、Scale。
6. 靠墙摆放时与墙面保持约 0.005 m 的间距，避免闪烁。若看到纯白背面，绕 Y 轴转 180 度。
若曾用旧扭曲版创建过 Prefab，请重新执行第 3 步以更新材质和网格。

配置脚本为这三张海报创建独立材质、网格及 Prefab，不修改正常版，不自动切换场景。
支持 URP/Lit 和 Built-in Standard。HDRP 需手动设置材质。
GLB 适用于支持 glTF 的软件；Unity 默认不直接导入 GLB，可使用配置脚本或 OBJ。
OBJ、MTL 与 Textures 文件夹应保持原相对位置。

验证
已检查 OBJ 索引、法线、UV、模型尺寸、GLB 内嵌 PNG、三张最终贴图一致性及 ZIP 完整解压。
已核对三张模型的顶点位置和 UV 与各自正常版完全一致。
当前环境没有 Unity 编辑器，配置脚本未在 Unity 内编译运行验证。
