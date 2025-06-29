using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GorillaPortraits.Behaviours;

namespace GorillaPortraits.Utilities
{
    internal class TaskYieldUtils
    {
        public static async Task Yield(UnityWebRequest webRequest)
        {
            var completionSource = new TaskCompletionSource<UnityWebRequest>();
            PhotoManager.Instance.StartCoroutine(AwaitWebRequestCoroutine(webRequest, completionSource));
            await completionSource.Task;
        }

        public static async Task Yield(YieldInstruction instruction)
        {
            var completionSource = new TaskCompletionSource<YieldInstruction>();
            PhotoManager.Instance.StartCoroutine(AwaitInstructionCorouutine(instruction, completionSource));
            await completionSource.Task;
        }

        private static IEnumerator AwaitWebRequestCoroutine(UnityWebRequest webRequest, TaskCompletionSource<UnityWebRequest> completionSource)
        {
            yield return webRequest.SendWebRequest();
            completionSource.SetResult(webRequest);
        }

        private static IEnumerator AwaitInstructionCorouutine(YieldInstruction instruction, TaskCompletionSource<YieldInstruction> completionSource)
        {
            yield return instruction;
            completionSource.SetResult(instruction);
        }
    }
}
