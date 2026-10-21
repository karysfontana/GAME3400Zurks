LAUNDROMAT BENCH — MODEL + TEXTURE

导入：Assets > Import Package > Custom Package，选择本文件并点 Import。
等待 Unity 编译完成，然后将 Assets/LaundromatBench/Prefabs/Laundromat_Bench 拖入场景。
若材质显示粉色：Tools > Laundromat Bench > Refresh Materials。

尺寸（长 X × 深 Z × 高 Y）：1.2192 × 0.3048 × 0.1524 米，即 4 × 1 × 0.5 ft。
保持根物体 Scale=(1,1,1) 即为规定尺寸。Pivot 位于地面中央。
注意：按任务表原尺寸制作，总高仅 15.24 cm。确认队伍要求后再改变高度。

内容：3 块木质椅面、4 条金属腿、橡胶脚垫、承托横梁和下部加强杆。
每个构件均为独立 Unity Cube，具有 BoxCollider，可在 Prefab Mode 编辑位置和尺寸。
包含原创程序生成木纹 PNG、Wood/Charcoal_Metal/Rubber 三种材质。
支持 Built-in 和 URP；Editor 脚本只自动适配本资源包三种材质的 Shader。
不依赖 ProBuilder；这不是包含 ProBuilderMesh 组件的模型。
无需挂载运行时脚本，无第三方模型或贴图。HDRP 未配置。

作者说明：由 ChatGPT 按用户规格生成，可按课程要求披露 AI 辅助，不能描述为完全手工制作。
验证：已检查文件结构、引用和几何尺寸；未在 Unity Editor 中实际导入运行。
