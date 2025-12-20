- Add following Nuget Package Source in Visual Studio
	- https://aiinfra.pkgs.visualstudio.com/PublicPackages/_packaging/ORT/nuget/v3/index.json

- winget install llama.cpp
	- llama-cli -hf LiquidAI/LFM2-1.2B-Tool-GGUF
	- For me it used ggml_vulkan and downloaded the model at
	- C:\Users\khurram\AppData\Local\llama.cpp\LiquidAI_LFM2-1.2B-Tool-GGUF_LFM2-1.2B-Tool-Q4_K_M.gguf

- Install Hugging Face CLI
	- https://huggingface.co/docs/huggingface_hub/en/guides/cli
	- Set HF_TOKEN for more speed; the hugging face token

**Interesting Resources**
- https://github.com/sangyuxiaowu/LLamaWorker
	- LlamaSharp based OpenAI Compatible Inference/Web/AI Server