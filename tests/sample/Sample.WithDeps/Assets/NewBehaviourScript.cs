using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Wooga.Lambda.Control.Concurrent;
using Wooga.Lambda.Network.Transport;
using Wooga.Lambda.Network;
using Wooga.Lambda.Data;
using Wooga.Lambda.Control.Monad;

public class NewBehaviourScript : MonoBehaviour {

	static HttpClient Http = WebRequestTransport.CreateHttpClient();

	static Async<string> LoadGoogle()
	{
		return Http.GetAsync ("http://www.google.com")
				   .Map<HttpResponse,string> (r =>
					{
						return r.Body
								.Map<ImmutableList<byte>,string>(b=>System.Text.Encoding.UTF8.GetString(b.ToArray()))
								.ValueOr("");
					})
				   .Map<string,string>(s=>
                    {
						int worker = 0;
						int completion = 0;
						System.Threading.ThreadPool.GetMaxThreads(out worker, out completion);
					 	Debug.Log("max: worker=" + worker + " completion:" + completion);
						System.Threading.ThreadPool.GetMinThreads(out worker, out completion);
						Debug.Log("min: worker=" + worker + " completion:" + completion);
						Debug.Log("thread: "+System.Threading.Thread.CurrentThread.ManagedThreadId);
						Debug.Log("respns: \n"+s);
						return s;
					});
	}

	// Use this for initialization
	void Start ()
	{
		System.Threading.ThreadPool.SetMaxThreads (16, 16);
		System.Threading.ThreadPool.SetMinThreads (16, 16);
//		System.Threading.ThreadPool.GetAvailableThreads
//		var asyncs = new List<Async<string>> ();
		Async.Start(()=>
        {
			for (var i = 0; i < 80; i++)
			{
				Debug.Log("start: " + i);
				LoadGoogle().Start();
			}
			return Unit.Default;
		});


//		ImmutableList.Create(asyncs).Parallel().Start();
	}

	// Update is called once per frame
	void Update () {

	}
}
