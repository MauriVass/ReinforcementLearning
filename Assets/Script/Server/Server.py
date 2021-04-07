import random
import string
import cherrypy
import json 

class Network():
	def __init__(self,a):
		self.a = a

	def fit(self):
		return 'fit' + self.a

	def predict(self):
		return 'predict' + self.a

class CalculatorWebService(object):
	#Required to be accessable online
	exposed=True

	def __init__(self):
		self.net = Network('1')

	@cherrypy.expose
	def FIT(self):
		msg = cherrypy.request.body.readline().decode("utf-8")
		print(msg)
		b = self.net.fit()
		output = b + msg
		return output

	def PREDICT(self,*path,**query):
		msg = cherrypy.request.body.read()
		output = net.predict() + msg
		return output

	def GET(self,*path,**query):
		output = 'Get'
		return output

	def POST(self,*path,**query):
		msg = cherrypy.request.body.read()
		print(msg)
		output = 'Post'
		return output

	def PUT(self,*path,**query):
		msg = cherrypy.request.body.read().decode("utf-8") 
		msg = msg.split('.')
		result = {'action':msg[4], 'value':[0.35,0]}
		output = json.dumps(result)
		return output

	def DELETE(self,*path,**query):
		output = 'Get'
		return output

#'request.dispatch': cherrypy.dispatch.MethodDispatcher() => switch from default URL to HTTP compliant approch
conf = { '/': {	'request.dispatch': cherrypy.dispatch.MethodDispatcher(),
								'tools.sessions.on':True} 
					}
cherrypy.tree.mount(CalculatorWebService(), '/', conf)

cherrypy.config.update({'servet.socket_host':'0.0.0.0'})
cherrypy.config.update({'servet.socket_port':'8080'})

cherrypy.engine.start()
cherrypy.engine.block()
